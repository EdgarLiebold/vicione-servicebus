namespace ViciOne.ServiceBus.SqlTransport.Topology;

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
    /// Gets the topic subscriptions value.
    /// </summary>
    TopicToTopicSubscription[] TopicSubscriptions { get; }
    /// <summary>
    /// Gets the queue subscriptions value.
    /// </summary>
    TopicToQueueSubscription[] QueueSubscriptions { get; }
}
