using ViciOne.ServiceBus.ActiveMq.Topology;

namespace ViciOne.ServiceBus.ActiveMq.Configuration;

/// <summary>Configures an ActiveMQ topic entity.</summary>
public class ActiveMqTopicConfigurator :
    EntityConfigurator,
    IActiveMqTopicConfigurator,
    Topic
{
    /// <summary>Creates a topic configurator from explicit entity settings.</summary>
    /// <param name="topicName">The topic name.</param>
    /// <param name="durable">Whether the topic persists across broker restarts.</param>
    /// <param name="autoDelete">Whether the broker removes the topic when it is no longer used.</param>
    public ActiveMqTopicConfigurator(string topicName, bool durable = true, bool autoDelete = false)
        : base(topicName, durable, autoDelete)
    {
    }

    /// <summary>Creates a topic configurator by copying existing topic settings.</summary>
    /// <param name="source">The topic settings to copy.</param>
    public ActiveMqTopicConfigurator(Topic source)
        : base(source.EntityName, source.Durable, source.AutoDelete)
    {
    }

    /// <summary>Gets the topic address type.</summary>
    protected override ActiveMqEndpointAddress.AddressType AddressType => ActiveMqEndpointAddress.AddressType.Topic;
}
