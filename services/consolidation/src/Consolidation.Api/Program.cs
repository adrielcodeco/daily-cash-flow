// @anchors
//   code: CNAPI
//   layer: host
//   updated_at: 2026-11-05
using Consolidation.Core;
using Consolidation.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Consolidation")
    ?? throw new InvalidOperationException("ConnectionStrings:Consolidation is required.");

var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379,abortConnect=false";

if (args.Contains("--migrate"))
{
    DatabaseMigrator.Run(connectionString);
    return;
}

builder.Services.AddNpgsqlDataSource(connectionString);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IBalanceStore, PostgresBalanceStore>();
builder.Services.AddSingleton<IBalanceCache>(RedisBalanceCache.Connect(redisConnection));
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

var app = builder.Build();

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
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = check => check.Name == "live" });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

// Local placeholder until token validation lands in milestone M6.
static bool TryReadMerchant(HttpContext context, out Guid merchantId) =>
    Guid.TryParse(context.Request.Headers["X-Merchant-Id"], out merchantId);

static IResult UnknownMerchant() =>
    Results.Problem(title: "The merchant could not be identified.", statusCode: StatusCodes.Status401Unauthorized);

app.MapGet("/v1/balances/daily", async (DateOnly date, HttpContext context, GetDailyBalance getDailyBalance, CancellationToken cancellationToken) =>
    TryReadMerchant(context, out var merchantId)
        ? Results.Ok(await getDailyBalance.ExecuteAsync(merchantId, date, cancellationToken))
        : UnknownMerchant());

app.MapGet("/v1/balances/period", async (DateOnly start, DateOnly end, HttpContext context, GetPeriodBalance getPeriodBalance, CancellationToken cancellationToken) =>
    TryReadMerchant(context, out var merchantId)
        ? Results.Ok(await getPeriodBalance.ExecuteAsync(merchantId, start, end, cancellationToken))
        : UnknownMerchant());

await app.RunAsync();
