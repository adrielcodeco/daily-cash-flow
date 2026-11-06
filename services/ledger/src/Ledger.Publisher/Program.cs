// @anchors
//   code: LDPUB
//   layer: host
//   updated_at: 2026-11-05
using Ledger.Core;
using Ledger.Infrastructure;
using Ledger.Publisher;

const int RepublishBatchSize = 500;

var builder = Host.CreateApplicationBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Ledger")
    ?? throw new InvalidOperationException("ConnectionStrings:Ledger is required.");
var projectId = builder.Configuration["PubSub:ProjectId"]
    ?? throw new InvalidOperationException("PubSub:ProjectId is required.");
var topicId = builder.Configuration["PubSub:TopicId"]
    ?? throw new InvalidOperationException("PubSub:TopicId is required.");

builder.Services.AddNpgsqlDataSource(connectionString);

if (args.Contains("--republish"))
{
    builder.Services.AddSingleton<IEntryStore, PostgresEntryStore>();
    builder.Services.AddSingleton<RepublishEntries>();
    using var republishHost = builder.Build();
    var total = await republishHost.Services.GetRequiredService<RepublishEntries>().ExecuteAsync(RepublishBatchSize, CancellationToken.None);
    Console.WriteLine($"{total} entries queued for publishing.");
    return;
}

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IOutbox, PostgresOutbox>();
builder.Services.AddSingleton<IEventPublisher>(await PubSubEventPublisher.CreateAsync(projectId, topicId));
builder.Services.AddSingleton<OutboxRelay>();
builder.Services.AddHostedService<OutboxWorker>();

await builder.Build().RunAsync();
