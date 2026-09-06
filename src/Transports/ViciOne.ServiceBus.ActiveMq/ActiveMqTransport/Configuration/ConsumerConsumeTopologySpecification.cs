using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.ActiveMq.Topology;

namespace ViciOne.ServiceBus.ActiveMq.Configuration;

/// <summary>Connects an ActiveMQ virtual topic to a receive endpoint through a consumer queue or named subscription.</summary>
public class ConsumerConsumeTopologySpecification :
    ActiveMqTopicBindingConfigurator,
    IActiveMqConsumeTopologySpecification
{
    readonly IActiveMqConsumerEndpointQueueNameFormatter? _consumerEndpointQueueNameFormatter;

    /// <summary>Creates a virtual-topic consume specification from explicit topic settings.</summary>
    /// <param name="topicName">The topic name.</param>
    /// <param name="consumerEndpointQueueNameFormatter">An optional formatter for the consumer queue or subscription name.</param>
    /// <param name="durable">Whether the topic and consumer queue persist across broker restarts.</param>
    /// <param name="autoDelete">Whether the broker removes the topic when it is no longer used.</param>
    public ConsumerConsumeTopologySpecification(string topicName, IActiveMqConsumerEndpointQueueNameFormatter? consumerEndpointQueueNameFormatter,
        bool durable = true, bool autoDelete = false)
        : base(topicName, durable, autoDelete)
    {
        _consumerEndpointQueueNameFormatter = consumerEndpointQueueNameFormatter;
    }

    /// <summary>Creates a virtual-topic consume specification from existing topic settings.</summary>
    /// <param name="topic">The topic settings to copy.</param>
    /// <param name="consumerEndpointQueueNameFormatter">An optional formatter for the consumer queue or subscription name.</param>
    public ConsumerConsumeTopologySpecification(Topic topic, IActiveMqConsumerEndpointQueueNameFormatter? consumerEndpointQueueNameFormatter)
        : base(topic)
    {
        _consumerEndpointQueueNameFormatter = consumerEndpointQueueNameFormatter;
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        return Enumerable.Empty<ValidationResult>();
    }

    /// <summary>Adds the virtual topic and its consumer queue or named Artemis subscription to a receive topology.</summary>
    /// <param name="builder">The receive-topology builder.</param>
    public void Apply(IReceiveEndpointBrokerTopologyBuilder builder)
    {
        var destinationQueue = builder.Queue.Queue;

        var topicName = EntityName;

        var consumerEndpointQueueName = _consumerEndpointQueueNameFormatter != null
            ? _consumerEndpointQueueNameFormatter.Format(topicName, destinationQueue.EntityName)
            : $"Consumer.{destinationQueue.EntityName}.{EntityName}";

        var topic = builder.CreateTopic(EntityName, Durable, AutoDelete);

        // Artemis FQQNs select an existing queue for receiving; they do not declare its routing
        // type. Creating that FQQN through the queue API therefore produces an ANYCAST queue,
        // while a publisher sends this virtual topic as MULTICAST and can never reach it. A
        // named shared topic subscription lets the AMQP provider declare the matching multicast
        // subscription queue and preserves one logical endpoint across bus instances.
        if (_consumerEndpointQueueNameFormatter is IActiveMqTopicSubscriptionNameFormatter)
        {
            _ = builder.BindConsumer(topic, null, Selector, consumerEndpointQueueName, shared: true);
            return;
        }

        var queue = builder.CreateQueue(consumerEndpointQueueName, destinationQueue.Durable, destinationQueue.AutoDelete);

        _ = builder.BindConsumer(topic, queue, Selector);
    }
}
