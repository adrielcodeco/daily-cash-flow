// @anchors
//   code: PSEPB
//   ref: OBRLY
//   updated_at: 2026-11-06
using System.Diagnostics;
using Google.Api.Gax;
using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using Ledger.Core;

namespace Ledger.Infrastructure;

public sealed class PubSubEventPublisher(PublisherClient client) : IEventPublisher, IAsyncDisposable
{
    public static async Task<PubSubEventPublisher> CreateAsync(string projectId, string topicId)
    {
        var client = await new PublisherClientBuilder
        {
            TopicName = TopicName.FromProjectTopic(projectId, topicId),
            EmulatorDetection = EmulatorDetection.EmulatorOrProduction,
        }.BuildAsync();

        return new PubSubEventPublisher(client);
    }

    public async Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        using var activity = OutboxTelemetry.Source.StartActivity("outbox publish", ActivityKind.Producer, message.TraceContext);
        activity?.SetTag("eventId", message.Id);

        var pubsubMessage = new PubsubMessage { Data = ByteString.CopyFromUtf8(message.Payload) };
        pubsubMessage.Attributes["eventId"] = message.Id.ToString();
        pubsubMessage.Attributes["type"] = message.Type;

        var traceContext = activity?.Id ?? message.TraceContext;
        if (traceContext is not null)
        {
            pubsubMessage.Attributes["traceparent"] = traceContext;
        }

        await client.PublishAsync(pubsubMessage).WaitAsync(cancellationToken);
        OutboxTelemetry.Published.Add(1);
    }

    public async ValueTask DisposeAsync() => await client.DisposeAsync();
}
