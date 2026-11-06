// @anchors
//   code: LDAPI
//   layer: host
//   updated_at: 2026-11-05
using Ledger.Api;
using Ledger.Core;
using Ledger.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

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
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks()
    .AddCheck("live", () => HealthCheckResult.Healthy())
    .AddAsyncCheck("database", async cancellationToken =>
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return HealthCheckResult.Healthy();
    }, tags: ["ready"]);

var app = builder.Build();

app.UseExceptionHandler(handler => handler.Run(async context =>
{
    var failure = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var (status, title) = failure is DomainRuleException rule
        ? (StatusCodes.Status422UnprocessableEntity, rule.Message)
        : failure is BadHttpRequestException
            ? (StatusCodes.Status400BadRequest, "The request is invalid.")
            : (StatusCodes.Status500InternalServerError, "An unexpected error occurred.");

    await Results.Problem(title: title, statusCode: status, type: (failure as DomainRuleException)?.Code).ExecuteAsync(context);
}));

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = check => check.Name == "live" });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
app.MapEntryEndpoints();

await app.RunAsync();
