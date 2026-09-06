using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>Provides an immutable snapshot of ActiveMQ topics, queues, and consumer bindings.</summary>
public class ActiveMqBrokerTopology :
    BrokerTopology
{
    /// <summary>Creates a broker-topology snapshot from the supplied entity sequences.</summary>
    /// <param name="topics">The declared topics.</param>
    /// <param name="queues">The declared queues.</param>
    /// <param name="consumers">The topic-to-queue or direct-topic consumer bindings.</param>
    public ActiveMqBrokerTopology(IEnumerable<Topic> topics, IEnumerable<Queue> queues, IEnumerable<Consumer> consumers)
    {
        Topics = topics.ToArray();
        Queues = queues.ToArray();
        Consumers = consumers.ToArray();
    }

    /// <summary>Gets the topic snapshot.</summary>
    public Topic[] Topics { get; }
    /// <summary>Gets the queue snapshot.</summary>
    public Queue[] Queues { get; }
    /// <summary>Gets the consumer-binding snapshot.</summary>
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
                binding.Selector,
                binding.ConsumerName,
                binding.IsShared
            });
        }
    }
}
