using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.ActiveMq.Topology;

namespace ViciOne.ServiceBus.ActiveMq.Configuration;

/// <summary>
/// Provides a consumer consume topic topology specification implementation.
/// </summary>
public class ConsumerConsumeTopicTopologySpecification :
    ActiveMqTopicBindingConfigurator,
    IActiveMqConsumeTopologySpecification
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="topicName">The topic name value.</param>
    /// <param name="durable">The durable value.</param>
    /// <param name="autoDelete">The auto delete value.</param>
    public ConsumerConsumeTopicTopologySpecification(string topicName, bool durable = true, bool autoDelete = false)
        : base(topicName, durable, autoDelete)
    {
    }

    /// <summary>
    /// If set to <c>true</c>, the consumer is shared between multiple receive endpoints.
    /// It means if you have multiple instances of the application a broker will balance messages between them.
    /// </summary>
    /// <remarks>
    /// Shared Durable subscription is supported only by ActiveMQ Artemis Broker and AMQP. If your broker is ActiveMQ
    /// or you are using OpenWire protocol (tcp|ssl://host:port or activemq://host:port URI) this is not supported.
    /// </remarks>
    public bool Shared { get; set; }

    /// <summary>
    /// The consumer name, if specified
    /// </summary>
    public string? ConsumerName { get; set; }

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
        var topic = builder.CreateTopic(EntityName, Durable, AutoDelete);

        _ = builder.BindConsumer(topic, null, Selector, ConsumerName, Shared);
    }
}
