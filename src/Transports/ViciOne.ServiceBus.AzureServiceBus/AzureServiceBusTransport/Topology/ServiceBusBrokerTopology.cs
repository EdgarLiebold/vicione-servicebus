using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>Provides Azure Service Bus entity declarations and relationships in mutable arrays.</summary>
public class ServiceBusBrokerTopology :
    BrokerTopology
{
    /// <summary>Materializes each topology collection.</summary>
    /// <param name="topics">The topic declarations.</param>
    /// <param name="subscriptions">The consumer subscription declarations.</param>
    /// <param name="queues">The queue declarations.</param>
    /// <param name="queueSubscriptions">The topic-to-queue forwarding relationships.</param>
    /// <param name="topicSubscriptions">The topic-to-topic forwarding relationships.</param>
    public ServiceBusBrokerTopology(IEnumerable<Topic> topics, IEnumerable<Subscription> subscriptions, IEnumerable<Queue> queues,
        IEnumerable<QueueSubscription> queueSubscriptions, IEnumerable<TopicSubscription> topicSubscriptions)
    {
        Topics = topics.ToArray();
        Queues = queues.ToArray();
        Subscriptions = subscriptions.ToArray();
        QueueSubscriptions = queueSubscriptions.ToArray();
        TopicSubscriptions = topicSubscriptions.ToArray();
    }

    /// <summary>Gets the topic declarations.</summary>
    public Topic[] Topics { get; }
    /// <summary>Gets the queue declarations.</summary>
    public Queue[] Queues { get; }
    /// <summary>Gets the consumer subscription declarations.</summary>
    public Subscription[] Subscriptions { get; }
    /// <summary>Gets the topic-to-queue forwarding relationships.</summary>
    public QueueSubscription[] QueueSubscriptions { get; }
    /// <summary>Gets the topic-to-topic forwarding relationships.</summary>
    public TopicSubscription[] TopicSubscriptions { get; }

    void IProbeSite.Probe(ProbeContext context)
    {
        foreach (var topic in Topics)
        {
            var exchangeScope = context.CreateScope("topic");
            exchangeScope.Set(new
            {
                topic.CreateTopicOptions.Name,
                topic.CreateTopicOptions.EnablePartitioning,
                topic.CreateTopicOptions.SupportOrdering,
                topic.CreateTopicOptions.EnableBatchedOperations,
                topic.CreateTopicOptions.RequiresDuplicateDetection,
                topic.CreateTopicOptions.AutoDeleteOnIdle,
                topic.CreateTopicOptions.DefaultMessageTimeToLive,
                topic.CreateTopicOptions.DuplicateDetectionHistoryTimeWindow,
                topic.CreateTopicOptions.MaxSizeInMegabytes
            });
        }

        foreach (var queue in Queues)
        {
            var exchangeScope = context.CreateScope("queue");
            exchangeScope.Set(new
            {
                queue.CreateQueueOptions.Name,
                queue.CreateQueueOptions.EnablePartitioning,
                queue.CreateQueueOptions.EnableBatchedOperations,
                queue.CreateQueueOptions.ForwardTo,
                queue.CreateQueueOptions.LockDuration,
                queue.CreateQueueOptions.MaxDeliveryCount,
                queue.CreateQueueOptions.RequiresSession,
                queue.CreateQueueOptions.RequiresDuplicateDetection,
                queue.CreateQueueOptions.AutoDeleteOnIdle,
                queue.CreateQueueOptions.DefaultMessageTimeToLive,
                queue.CreateQueueOptions.DuplicateDetectionHistoryTimeWindow,
                queue.CreateQueueOptions.MaxSizeInMegabytes,
                queue.CreateQueueOptions.ForwardDeadLetteredMessagesTo,
                queue.CreateQueueOptions.DeadLetteringOnMessageExpiration
            });
        }

        foreach (var subscription in Subscriptions)
        {
            var subscriptionScope = context.CreateScope("subscription");
            subscriptionScope.Set(GetSubscriptionValues(subscription));
        }

        foreach (var queueSubscription in QueueSubscriptions)
        {
            var queueSubscriptionScope = context.CreateScope("queueSubscription");
            queueSubscriptionScope.Set(GetSubscriptionValues(queueSubscription.Subscription));
        }

        foreach (var topicSubscription in TopicSubscriptions)
        {
            var topicSubscriptionScope = context.CreateScope("topicSubscription");
            topicSubscriptionScope.Set(GetSubscriptionValues(topicSubscription.Subscription));
        }
    }

    static object GetSubscriptionValues(Subscription subscription)
    {
        return new
        {
            subscription.CreateSubscriptionOptions.SubscriptionName,
            subscription.CreateSubscriptionOptions.EnableBatchedOperations,
            subscription.CreateSubscriptionOptions.ForwardTo,
            subscription.CreateSubscriptionOptions.LockDuration,
            subscription.CreateSubscriptionOptions.MaxDeliveryCount,
            subscription.CreateSubscriptionOptions.RequiresSession,
            subscription.CreateSubscriptionOptions.AutoDeleteOnIdle,
            subscription.CreateSubscriptionOptions.DefaultMessageTimeToLive,
            subscription.CreateSubscriptionOptions.ForwardDeadLetteredMessagesTo,
            subscription.CreateSubscriptionOptions.DeadLetteringOnMessageExpiration
        };
    }
}
