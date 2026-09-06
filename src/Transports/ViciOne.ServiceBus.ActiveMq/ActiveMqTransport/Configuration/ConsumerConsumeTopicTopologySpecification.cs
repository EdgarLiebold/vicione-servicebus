using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.ActiveMq.Topology;

namespace ViciOne.ServiceBus.ActiveMq.Configuration;

/// <summary>Declares a named ActiveMQ topic subscription consumed directly by a receive endpoint.</summary>
public class ConsumerConsumeTopicTopologySpecification :
    ActiveMqTopicBindingConfigurator,
    IActiveMqConsumeTopologySpecification
{
    /// <summary>Creates a topic-consumer topology specification.</summary>
    /// <param name="topicName">The topic name.</param>
    /// <param name="durable">Whether the topic and named subscription are durable.</param>
    /// <param name="autoDelete">Whether the topic is removed automatically when unused.</param>
    public ConsumerConsumeTopicTopologySpecification(string topicName, bool durable = true, bool autoDelete = false)
        : base(topicName, durable, autoDelete)
    {
    }

    /// <summary>
    /// Gets or sets whether multiple receive endpoints share the named topic subscription so the broker
    /// distributes messages among application instances.
    /// </summary>
    /// <remarks>
    /// Shared subscriptions require ActiveMQ Artemis over AMQP; ActiveMQ Classic and OpenWire do not support them.
    /// </remarks>
    public bool Shared { get; set; }

    /// <summary>Gets or sets the native subscription name.</summary>
    public string? ConsumerName { get; set; }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        return Enumerable.Empty<ValidationResult>();
    }

    /// <summary>Adds the topic and its named consumer subscription to a receive topology.</summary>
    /// <param name="builder">The receive-topology builder.</param>
    public void Apply(IReceiveEndpointBrokerTopologyBuilder builder)
    {
        var topic = builder.CreateTopic(EntityName, Durable, AutoDelete);

        _ = builder.BindConsumer(topic, null, Selector, ConsumerName, Shared);
    }
}
