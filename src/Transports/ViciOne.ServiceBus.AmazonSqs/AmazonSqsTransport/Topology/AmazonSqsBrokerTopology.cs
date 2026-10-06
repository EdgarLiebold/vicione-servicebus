using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>Represents the Amazon SNS topics, Amazon SQS queues, and subscriptions required by an endpoint.</summary>
public class AmazonSqsBrokerTopology :
    BrokerTopology
{
    /// <summary>Copies the supplied topology sequences into arrays.</summary>
    /// <param name="topics">The Amazon SNS topics.</param>
    /// <param name="queues">The Amazon SQS queues.</param>
    /// <param name="queueSubscriptions">The topic-to-queue subscriptions.</param>
    public AmazonSqsBrokerTopology(IEnumerable<Topic> topics, IEnumerable<Queue> queues, IEnumerable<QueueSubscription> queueSubscriptions)
    {
        Topics = topics.ToArray();
        Queues = queues.ToArray();
        QueueSubscriptions = queueSubscriptions.ToArray();
    }

    /// <summary>Gets the Amazon SNS topics.</summary>
    public Topic[] Topics { get; }
    /// <summary>Gets the Amazon SQS queues.</summary>
    public Queue[] Queues { get; }
    /// <summary>Gets the topic-to-queue subscriptions.</summary>
    public QueueSubscription[] QueueSubscriptions { get; }

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

    }
}
