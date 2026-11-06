// @anchors
//   code: CNAPI
//   layer: host
//   updated_at: 2026-11-05
using Consolidation.Core;
using Consolidation.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

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
builder.Services.AddSingleton<IBalanceStore, PostgresBalanceStore>();
builder.Services.AddSingleton<GetDailyBalance>();
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

app.UseExceptionHandler();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = check => check.Name == "live" });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.MapGet("/v1/balances/daily", async (DateOnly date, HttpContext context, GetDailyBalance getDailyBalance, CancellationToken cancellationToken) =>
{
    // Local placeholder until token validation lands in milestone M6.
    if (!Guid.TryParse(context.Request.Headers["X-Merchant-Id"], out var merchantId))
    {
        return Results.Problem(title: "The merchant could not be identified.", statusCode: StatusCodes.Status401Unauthorized);
    }

    var balance = await getDailyBalance.ExecuteAsync(merchantId, date, cancellationToken);
    return Results.Ok(balance);
});

await app.RunAsync();
