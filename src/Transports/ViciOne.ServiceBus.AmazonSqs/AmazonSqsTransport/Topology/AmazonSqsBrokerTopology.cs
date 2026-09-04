using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>
/// Provides an amazon sqs broker topology implementation.
/// </summary>
public class AmazonSqsBrokerTopology :
    BrokerTopology
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="exchanges">The exchanges value.</param>
    /// <param name="queues">The queues value.</param>
    /// <param name="queueSubscriptions">The queue subscriptions value.</param>
    /// <param name="topicSubscriptions">The topic subscriptions value.</param>
    public AmazonSqsBrokerTopology(IEnumerable<Topic> exchanges, IEnumerable<Queue> queues, IEnumerable<QueueSubscription> queueSubscriptions,
        IEnumerable<TopicSubscription> topicSubscriptions)
    {
        Topics = exchanges.ToArray();
        Queues = queues.ToArray();
        QueueSubscriptions = queueSubscriptions.ToArray();
        TopicSubscriptions = topicSubscriptions.ToArray();
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
    /// Gets the queue subscriptions value.
    /// </summary>
    public QueueSubscription[] QueueSubscriptions { get; }
    /// <summary>
    /// Gets the topic subscriptions value.
    /// </summary>
    public TopicSubscription[] TopicSubscriptions { get; }

    void IProbeSite.Probe(ProbeContext context)
    {
        foreach (var topic in Topics)
        {
            var topicScope = context.CreateScope("topic");
            topicScope.Set(new
            {
                Name = topic.EntityName,
                topic.Durable,
                topic.AutoDelete
            });
        }

        foreach (var queue in Queues)
        {
            var queueScope = context.CreateScope("queue");
            queueScope.Set(new
            {
                Name = queue.EntityName,
                queue.Durable,
                queue.AutoDelete
            });
        }

        foreach (var subscription in QueueSubscriptions)
        {
            var subscriptionScope = context.CreateScope("queueSubscription");
            subscriptionScope.Set(new
            {
                Source = subscription.Source.EntityName,
                Destination = subscription.Destination.EntityName
            });
        }

        foreach (var subscription in TopicSubscriptions)
        {
            var subscriptionScope = context.CreateScope("topicSubscription");
            subscriptionScope.Set(new
            {
                Source = subscription.Source.EntityName,
                Destination = subscription.Destination.EntityName
            });
        }
    }
}
