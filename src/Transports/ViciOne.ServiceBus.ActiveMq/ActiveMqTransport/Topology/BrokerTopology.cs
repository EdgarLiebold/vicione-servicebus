namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>
/// Defines the contract for broker topology.
/// </summary>
public interface BrokerTopology :
    IProbeSite
{
    /// <summary>
    /// Gets the topics value.
    /// </summary>
    Topic[] Topics { get; }
    /// <summary>
    /// Gets the queues value.
    /// </summary>
    Queue[] Queues { get; }
    /// <summary>
    /// Gets the consumers value.
    /// </summary>
    Consumer[] Consumers { get; }
}
