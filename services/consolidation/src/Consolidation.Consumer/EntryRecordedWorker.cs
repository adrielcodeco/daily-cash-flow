// @anchors
//   code: CNWRK
//   layer: host
//   updated_at: 2026-11-05
using System.Text.Json;
using Consolidation.Core;
using Google.Api.Gax;
using Google.Cloud.PubSub.V1;

namespace Consolidation.Consumer;

public sealed record EventSubscription(string ProjectId, string SubscriptionId);

public sealed class EntryRecordedWorker(
    EventSubscription subscription,
    ApplyEntryRecorded applyEntryRecorded,
    ILogger<EntryRecordedWorker> logger) : BackgroundService
{
    // Bounds the backlog drain after an outage, so the consumer cannot exhaust the database connections.
    private const int MaxEventsInFlight = 16;

    private static readonly JsonSerializerOptions EventJson = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var subscriber = await new SubscriberClientBuilder
        {
            SubscriptionName = SubscriptionName.FromProjectSubscription(subscription.ProjectId, subscription.SubscriptionId),
            EmulatorDetection = EmulatorDetection.EmulatorOrProduction,
            ClientCount = 1,
            Settings = new SubscriberClient.Settings
            {
                FlowControlSettings = new FlowControlSettings(maxOutstandingElementCount: MaxEventsInFlight, maxOutstandingByteCount: null),
            },
        }.BuildAsync(stoppingToken);

        stoppingToken.Register(() => subscriber.StopAsync(TimeSpan.FromSeconds(10)));
        await subscriber.StartAsync((message, cancellationToken) => HandleAsync(message, cancellationToken));
    }

    private async Task<SubscriberClient.Reply> HandleAsync(PubsubMessage message, CancellationToken cancellationToken)
    {
        try
        {
            var recorded = JsonSerializer.Deserialize<EntryRecorded>(message.Data.ToStringUtf8(), EventJson)
                ?? throw new JsonException("The event is empty.");

            await applyEntryRecorded.ExecuteAsync(recorded, cancellationToken);
            return SubscriberClient.Reply.Ack;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Event {MessageId} failed and will be redelivered.", message.MessageId);
            return SubscriberClient.Reply.Nack;
        }
    }
}
