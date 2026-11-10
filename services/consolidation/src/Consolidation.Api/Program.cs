// @anchors
//   code: CNAPI
//   ref: CNHTP
//   layer: host
//   updated_at: 2026-11-09
using Consolidation.Api;
using Consolidation.Core;
using Consolidation.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Consolidation")
    ?? throw new InvalidOperationException("ConnectionStrings:Consolidation is required.");

if (args.Contains("--migrate"))
{
    DatabaseMigrator.Run(connectionString);
    return;
}

builder.Services.AddNpgsqlDataSource(connectionString);
builder.Services.AddSingleton(TimeProvider.System);
builder.AddBalanceStore();
builder.Services.AddSingleton<GetDailyBalance>();
builder.Services.AddSingleton<GetPeriodBalance>();
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
    "consolidation-api",
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
    var (status, title, type) = failure switch
    {
        BalanceQueryException query => (StatusCodes.Status400BadRequest, query.Message, query.Code),
        BadHttpRequestException => (StatusCodes.Status400BadRequest, "The request is invalid.", null),
        _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", null),
    };

    await Results.Problem(title: title, statusCode: status, type: type).ExecuteAsync(context);
}));

// Refusals that carry no body of their own (a malformed request, a missing token, a missing scope) answer in the problem format too.
app.UseStatusCodePages();
app.UseApiSecurity();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = check => check.Name == "live" });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

static bool TryReadMerchant(HttpContext context, out Guid merchantId) =>
    Guid.TryParse(context.User.FindFirst(ApiSecurity.MerchantClaim)?.Value, out merchantId) && merchantId != Guid.Empty;

static IResult UnknownMerchant() =>
    Results.Problem(title: "The merchant could not be identified.", statusCode: StatusCodes.Status401Unauthorized);

app.MapGet("/v1/balances/daily", async (DateOnly date, HttpContext context, GetDailyBalance getDailyBalance, CancellationToken cancellationToken) =>
    TryReadMerchant(context, out var merchantId)
        ? Results.Ok(await getDailyBalance.ExecuteAsync(merchantId, date, cancellationToken))
        : UnknownMerchant()).RequireAuthorization(ApiSecurity.BalancesRead);

app.MapGet("/v1/balances/period", async (DateOnly start, DateOnly end, HttpContext context, GetPeriodBalance getPeriodBalance, CancellationToken cancellationToken) =>
    TryReadMerchant(context, out var merchantId)
        ? Results.Ok(await getPeriodBalance.ExecuteAsync(merchantId, start, end, cancellationToken))
        : UnknownMerchant()).RequireAuthorization(ApiSecurity.BalancesRead);

await app.RunAsync();
