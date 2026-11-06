// @anchors
//   code: CNCSM
//   layer: host
//   updated_at: 2026-11-06
using Consolidation.Consumer;
using Consolidation.Core;
using Consolidation.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);
var projectId = builder.Configuration["PubSub:ProjectId"]
    ?? throw new InvalidOperationException("PubSub:ProjectId is required.");
var topicId = builder.Configuration["PubSub:TopicId"]
    ?? throw new InvalidOperationException("PubSub:TopicId is required.");
var subscriptionId = builder.Configuration["PubSub:SubscriptionId"]
    ?? throw new InvalidOperationException("PubSub:SubscriptionId is required.");

if (args.Contains("--provision"))
{
    await PubSubProvisioner.RunAsync(projectId, topicId, subscriptionId);
    return;
}

var connectionString = builder.Configuration.GetConnectionString("Consolidation")
    ?? throw new InvalidOperationException("ConnectionStrings:Consolidation is required.");

var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379,abortConnect=false";

builder.Services.AddNpgsqlDataSource(connectionString);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IBalanceStore, PostgresBalanceStore>();

if (args.Contains("--reset"))
{
    builder.Services.AddSingleton<ResetBalances>();
    using var resetHost = builder.Build();
    await resetHost.Services.GetRequiredService<ResetBalances>().ExecuteAsync(CancellationToken.None);
    resetHost.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Audit")
        .LogInformation("Maintenance {Operation} finished.", "reset");
    return;
}

builder.Services.AddSingleton<IBalanceCache>(RedisBalanceCache.Connect(redisConnection));
builder.Services.AddSingleton<ApplyEntryRecorded>();
builder.Services.AddSingleton(new EventSubscription(projectId, subscriptionId));
builder.Services.AddHostedService<EntryRecordedWorker>();
builder.Services.AddServiceTelemetry("consolidation-consumer");

await builder.Build().RunAsync();
