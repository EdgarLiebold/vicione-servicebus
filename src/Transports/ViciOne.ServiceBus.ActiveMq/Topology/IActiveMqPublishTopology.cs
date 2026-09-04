using ViciOne.ServiceBus.ActiveMq.Topology;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Defines the contract for active mq publish topology.
/// </summary>
public interface IActiveMqPublishTopology :
    IPublishTopology
{
    /// <summary>
    /// Gets the virtual topic prefix value.
    /// </summary>
    string VirtualTopicPrefix { get; }

    /// <summary>
    /// Gets the virtual topic consumer pattern value.
    /// </summary>
    string VirtualTopicConsumerPattern { get; }

    /// <summary>
    /// Gets message topology.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    new IActiveMqMessagePublishTopology<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>
    /// Gets publish broker topology.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    BrokerTopology GetPublishBrokerTopology();
}
