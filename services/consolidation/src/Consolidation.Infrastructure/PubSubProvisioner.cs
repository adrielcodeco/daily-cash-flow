// @anchors
//   code: PSPRV
//   layer: infrastructure
//   updated_at: 2026-11-05
using Google.Api.Gax;
using Google.Cloud.PubSub.V1;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace Consolidation.Infrastructure;

/// <summary>
/// Creates the topics and the subscription before any event is published, because Pub/Sub
/// drops messages sent to a topic without subscriptions. Terraform owns this step in GCP.
/// </summary>
public static class PubSubProvisioner
{
    private const int MaxDeliveryAttempts = 5;

    public static async Task RunAsync(string projectId, string topicId, string subscriptionId)
    {
        var publisher = await new PublisherServiceApiClientBuilder { EmulatorDetection = EmulatorDetection.EmulatorOrProduction }.BuildAsync();
        var subscriber = await new SubscriberServiceApiClientBuilder { EmulatorDetection = EmulatorDetection.EmulatorOrProduction }.BuildAsync();

        var topic = TopicName.FromProjectTopic(projectId, topicId);
        var deadLetterTopic = TopicName.FromProjectTopic(projectId, $"{topicId}-dead-letter");

        await IgnoreExistingAsync(() => publisher.CreateTopicAsync(topic));
        await IgnoreExistingAsync(() => publisher.CreateTopicAsync(deadLetterTopic));
        await IgnoreExistingAsync(() => subscriber.CreateSubscriptionAsync(new Subscription
        {
            SubscriptionName = SubscriptionName.FromProjectSubscription(projectId, $"{subscriptionId}-dead-letter"),
            TopicAsTopicName = deadLetterTopic,
        }));
        await IgnoreExistingAsync(() => subscriber.CreateSubscriptionAsync(new Subscription
        {
            SubscriptionName = SubscriptionName.FromProjectSubscription(projectId, subscriptionId),
            TopicAsTopicName = topic,
            AckDeadlineSeconds = 30,
            DeadLetterPolicy = new DeadLetterPolicy { DeadLetterTopic = deadLetterTopic.ToString(), MaxDeliveryAttempts = MaxDeliveryAttempts },
            RetryPolicy = new RetryPolicy
            {
                MinimumBackoff = Duration.FromTimeSpan(TimeSpan.FromSeconds(1)),
                MaximumBackoff = Duration.FromTimeSpan(TimeSpan.FromSeconds(60)),
            },
        }));
    }

    private static async Task IgnoreExistingAsync<T>(Func<Task<T>> create)
    {
        try
        {
            await create();
        }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.AlreadyExists)
        {
        }
    }
}
