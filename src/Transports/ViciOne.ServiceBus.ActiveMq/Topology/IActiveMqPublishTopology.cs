using ViciOne.ServiceBus.ActiveMq.Topology;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Exposes ActiveMQ virtual-topic publish topology.</summary>
public interface IActiveMqPublishTopology :
    IPublishTopology
{
    /// <summary>Gets the prefix prepended to published message entity names.</summary>
    string VirtualTopicPrefix { get; }

    /// <summary>Gets the regular expression used to identify virtual-topic consumer destinations.</summary>
    string VirtualTopicConsumerPattern { get; }

    /// <summary>Gets publish topology for a message type.</summary>
    /// <typeparam name="T">The published message type.</typeparam>
    /// <returns>The ActiveMQ message publish topology.</returns>
    new IActiveMqMessagePublishTopology<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>Builds combined broker topology for all configured publish message types.</summary>
    /// <returns>The publish broker topology.</returns>
    BrokerTopology GetPublishBrokerTopology();
}
