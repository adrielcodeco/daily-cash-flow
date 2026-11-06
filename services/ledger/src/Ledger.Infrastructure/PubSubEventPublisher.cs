// @anchors
//   code: PSEPB
//   ref: OBRLY
//   updated_at: 2026-11-05
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
        var pubsubMessage = new PubsubMessage { Data = ByteString.CopyFromUtf8(message.Payload) };
        pubsubMessage.Attributes["eventId"] = message.Id.ToString();
        pubsubMessage.Attributes["type"] = message.Type;

        await client.PublishAsync(pubsubMessage).WaitAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync() => await client.DisposeAsync();
}
