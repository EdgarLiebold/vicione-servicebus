namespace ViciOne.ServiceBus.AmazonSqs.Topology;

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
    /// Gets the queue subscriptions value.
    /// </summary>
    QueueSubscription[] QueueSubscriptions { get; }
    /// <summary>
    /// Gets the topic subscriptions value.
    /// </summary>
    TopicSubscription[] TopicSubscriptions { get; }
}
