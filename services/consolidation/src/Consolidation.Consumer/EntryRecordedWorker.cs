// @anchors
//   code: CNWRK
//   layer: host
//   updated_at: 2026-11-06
using System.Diagnostics;
using System.Text.Json;
using Consolidation.Core;
using Consolidation.Infrastructure;
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
        message.Attributes.TryGetValue("traceparent", out var traceContext);
        using var activity = ConsolidationTelemetry.Source.StartActivity("entry.recorded process", ActivityKind.Consumer, traceContext);

        try
        {
            var recorded = JsonSerializer.Deserialize<EntryRecorded>(message.Data.ToStringUtf8(), EventJson)
                ?? throw new JsonException("The event is empty.");
            activity?.SetTag("eventId", recorded.EventId);

            var applied = await applyEntryRecorded.ExecuteAsync(recorded, cancellationToken);
            Count(applied ? "applied" : "repeated");
            if (applied)
            {
                ConsolidationTelemetry.BalanceLag.Record((DateTimeOffset.UtcNow - recorded.OccurredAt).TotalSeconds);
            }

            return SubscriberClient.Reply.Ack;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Count("error");
            activity?.SetStatus(ActivityStatusCode.Error);
            logger.LogError(exception, "Event {MessageId} failed and will be redelivered.", message.MessageId);
            return SubscriberClient.Reply.Nack;
        }
    }

    private static void Count(string result) =>
        ConsolidationTelemetry.ProcessedEvents.Add(1, new KeyValuePair<string, object?>("result", result));
}
