using ViciOne.ServiceBus.ActiveMq.Topology;

namespace ViciOne.ServiceBus.ActiveMq.Configuration;

/// <summary>
/// Provides an active mq topic configurator implementation.
/// </summary>
public class ActiveMqTopicConfigurator :
    EntityConfigurator,
    IActiveMqTopicConfigurator,
    Topic
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="topicName">The topic name value.</param>
    /// <param name="durable">The durable value.</param>
    /// <param name="autoDelete">The auto delete value.</param>
    public ActiveMqTopicConfigurator(string topicName, bool durable = true, bool autoDelete = false)
        : base(topicName, durable, autoDelete)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="source">The source value.</param>
    public ActiveMqTopicConfigurator(Topic source)
        : base(source.EntityName, source.Durable, source.AutoDelete)
    {
    }

    /// <summary>
    /// Gets the address type value.
    /// </summary>
    protected override ActiveMqEndpointAddress.AddressType AddressType => ActiveMqEndpointAddress.AddressType.Topic;
}
