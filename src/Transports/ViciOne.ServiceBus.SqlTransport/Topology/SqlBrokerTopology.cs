using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>Defines the topology for sql broker.</summary>
public class SqlBrokerTopology :
    BrokerTopology
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="topics">The topics.</param>
    /// <param name="topicSubscriptions">The topic subscriptions.</param>
    /// <param name="queues">The queues.</param>
    /// <param name="queueSubscriptions">The queue subscriptions.</param>
    public SqlBrokerTopology(IEnumerable<Topic> topics, IEnumerable<TopicToTopicSubscription> topicSubscriptions, IEnumerable<Queue> queues,
        IEnumerable<TopicToQueueSubscription> queueSubscriptions)
    {
        Topics = topics.ToArray();
        Queues = queues.ToArray();
        TopicSubscriptions = topicSubscriptions.ToArray();
        QueueSubscriptions = queueSubscriptions.ToArray();
    }

    /// <summary>Gets the topics.</summary>
    public Topic[] Topics { get; }
    /// <summary>Gets the queues.</summary>
    public Queue[] Queues { get; }
    /// <summary>Gets the topic subscriptions.</summary>
    public TopicToTopicSubscription[] TopicSubscriptions { get; }
    /// <summary>Gets the queue subscriptions.</summary>
    public TopicToQueueSubscription[] QueueSubscriptions { get; }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
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
                queue.AutoDeleteOnIdle,
                queue.MaxDeliveryCount
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
