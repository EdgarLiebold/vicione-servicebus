using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>
/// Provides an active mq broker topology implementation.
/// </summary>
public class ActiveMqBrokerTopology :
    BrokerTopology
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="topics">The topics value.</param>
    /// <param name="queues">The queues value.</param>
    /// <param name="consumers">The consumers value.</param>
    public ActiveMqBrokerTopology(IEnumerable<Topic> topics, IEnumerable<Queue> queues, IEnumerable<Consumer> consumers)
    {
        Topics = topics.ToArray();
        Queues = queues.ToArray();
        Consumers = consumers.ToArray();
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
    /// Gets the consumers value.
    /// </summary>
    public Consumer[] Consumers { get; }

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
                queue.AutoDelete
            });
        }

        foreach (var binding in Consumers)
        {
            var consumerScope = context.CreateScope("consumer");
            consumerScope.Set(new
            {
                Source = binding.Source.EntityName,
                Destination = binding.Destination?.EntityName,
                RoutingKey = binding.Selector,
                binding.ConsumerName,
                binding.IsShared
            });
        }
    }
}
