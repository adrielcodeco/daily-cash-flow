// @anchors
//   code: LDAPI
//   layer: host
//   updated_at: 2026-11-06
using Ledger.Api;
using Ledger.Core;
using Ledger.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Ledger")
    ?? throw new InvalidOperationException("ConnectionStrings:Ledger is required.");

if (args.Contains("--migrate"))
{
    DatabaseMigrator.Run(connectionString);
    return;
}

builder.Services.AddNpgsqlDataSource(connectionString);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IEntryStore, PostgresEntryStore>();
builder.Services.AddSingleton<RecordEntry>();
builder.Services.AddSingleton<ReverseEntry>();
builder.Services.AddSingleton<ListEntries>();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks()
    .AddCheck("live", () => HealthCheckResult.Healthy())
    .AddAsyncCheck("database", async cancellationToken =>
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return HealthCheckResult.Healthy();
    }, tags: ["ready"]);

builder.Services.AddServiceTelemetry(
    "ledger-api",
    tracing => tracing.AddAspNetCoreInstrumentation(options => options.Filter = context => !context.Request.Path.StartsWithSegments("/health")),
    metrics => metrics.AddAspNetCoreInstrumentation());
builder.AddApiSecurity();

// Successful requests are covered by metrics and traces, so they do not need logs.
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);

var app = builder.Build();

// Audit runs outside the exception handler, so it sees the final status of refused requests.
app.UseMiddleware<AuditMiddleware>();
app.UseExceptionHandler(handler => handler.Run(async context =>
{
    var failure = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var (status, title) = failure switch
    {
        DomainRuleException { Kind: RuleKind.NotFound } rule => (StatusCodes.Status404NotFound, rule.Message),
        DomainRuleException { Kind: RuleKind.Conflict } rule => (StatusCodes.Status409Conflict, rule.Message),
        DomainRuleException { Kind: RuleKind.BadRequest } rule => (StatusCodes.Status400BadRequest, rule.Message),
        DomainRuleException rule => (StatusCodes.Status422UnprocessableEntity, rule.Message),
        BadHttpRequestException => (StatusCodes.Status400BadRequest, "The request is invalid."),
        _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred."),
    };

    await Results.Problem(title: title, statusCode: status, type: (failure as DomainRuleException)?.Code).ExecuteAsync(context);
}));

app.UseApiSecurity();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = check => check.Name == "live" });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
app.MapEntryEndpoints();

await app.RunAsync();
