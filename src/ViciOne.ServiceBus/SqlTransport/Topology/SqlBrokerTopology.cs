using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>
/// Provides a sql broker topology implementation.
/// </summary>
public class SqlBrokerTopology :
    BrokerTopology
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="topics">The topics value.</param>
    /// <param name="topicSubscriptions">The topic subscriptions value.</param>
    /// <param name="queues">The queues value.</param>
    /// <param name="queueSubscriptions">The queue subscriptions value.</param>
    public SqlBrokerTopology(IEnumerable<Topic> topics, IEnumerable<TopicToTopicSubscription> topicSubscriptions, IEnumerable<Queue> queues,
        IEnumerable<TopicToQueueSubscription> queueSubscriptions)
    {
        Topics = topics.ToArray();
        Queues = queues.ToArray();
        TopicSubscriptions = topicSubscriptions.ToArray();
        QueueSubscriptions = queueSubscriptions.ToArray();
    }

    /// <summary>
    /// Gets the topics value.
    /// </summary>
    public Topic[] Topics { get; }
    /// <summary>
    /// Gets the queues value.
    /// </summary>
    public Queue[] Queues { get; }
    /// <summary>
    /// Gets the topic subscriptions value.
    /// </summary>
    public TopicToTopicSubscription[] TopicSubscriptions { get; }
    /// <summary>
    /// Gets the queue subscriptions value.
    /// </summary>
    public TopicToQueueSubscription[] QueueSubscriptions { get; }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        foreach (var topic in Topics)
        {
            var scope = context.CreateScope("topic");
            scope.Set(new { Name = topic.TopicName });
        }

        foreach (var queue in Queues)
        {
            var scope = context.CreateScope("queue");
            scope.Set(new
            {
                Name = queue.QueueName,
                queue.AutoDeleteOnIdle
            });
        }

        foreach (var subscription in TopicSubscriptions)
        {
            var scope = context.CreateScope("topic-subscription");
            scope.Set(new
            {
                Source = subscription.Source.TopicName,
                Destination = subscription.Destination.TopicName,
                subscription.SubscriptionType,
                subscription.RoutingKey
            });
        }

        foreach (var subscription in QueueSubscriptions)
        {
            var scope = context.CreateScope("queue-subscription");
            scope.Set(new
            {
                Source = subscription.Source.TopicName,
                Destination = subscription.Destination.QueueName,
                subscription.SubscriptionType,
                subscription.RoutingKey
            });
        }
    }
}
