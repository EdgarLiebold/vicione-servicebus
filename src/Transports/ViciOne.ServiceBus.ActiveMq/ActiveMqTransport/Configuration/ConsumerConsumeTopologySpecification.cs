using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.ActiveMq.Topology;

namespace ViciOne.ServiceBus.ActiveMq.Configuration;

/// <summary>
/// Used to by a Consumer virtual destination to the receive endpoint, via an additional message consumer
/// </summary>
public class ConsumerConsumeTopologySpecification :
    ActiveMqTopicBindingConfigurator,
    IActiveMqConsumeTopologySpecification
{
    readonly IActiveMqConsumerEndpointQueueNameFormatter? _consumerEndpointQueueNameFormatter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="topicName">The topic name value.</param>
    /// <param name="consumerEndpointQueueNameFormatter">The consumer endpoint queue name formatter value.</param>
    /// <param name="durable">The durable value.</param>
    /// <param name="autoDelete">The auto delete value.</param>
    public ConsumerConsumeTopologySpecification(string topicName, IActiveMqConsumerEndpointQueueNameFormatter? consumerEndpointQueueNameFormatter,
        bool durable = true, bool autoDelete = false)
        : base(topicName, durable, autoDelete)
    {
        _consumerEndpointQueueNameFormatter = consumerEndpointQueueNameFormatter;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="topic">The topic value.</param>
    /// <param name="consumerEndpointQueueNameFormatter">The consumer endpoint queue name formatter value.</param>
    public ConsumerConsumeTopologySpecification(Topic topic, IActiveMqConsumerEndpointQueueNameFormatter? consumerEndpointQueueNameFormatter)
        : base(topic)
    {
        _consumerEndpointQueueNameFormatter = consumerEndpointQueueNameFormatter;
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        return Enumerable.Empty<ValidationResult>();
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
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
