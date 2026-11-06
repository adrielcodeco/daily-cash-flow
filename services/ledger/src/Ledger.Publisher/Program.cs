// @anchors
//   code: LDPUB
//   layer: host
//   updated_at: 2026-11-05
using Ledger.Core;
using Ledger.Infrastructure;
using Ledger.Publisher;

var builder = Host.CreateApplicationBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Ledger")
    ?? throw new InvalidOperationException("ConnectionStrings:Ledger is required.");
var projectId = builder.Configuration["PubSub:ProjectId"]
    ?? throw new InvalidOperationException("PubSub:ProjectId is required.");
var topicId = builder.Configuration["PubSub:TopicId"]
    ?? throw new InvalidOperationException("PubSub:TopicId is required.");

builder.Services.AddNpgsqlDataSource(connectionString);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IOutbox, PostgresOutbox>();
builder.Services.AddSingleton<IEventPublisher>(await PubSubEventPublisher.CreateAsync(projectId, topicId));
builder.Services.AddSingleton<OutboxRelay>();
builder.Services.AddHostedService<OutboxWorker>();

await builder.Build().RunAsync();
