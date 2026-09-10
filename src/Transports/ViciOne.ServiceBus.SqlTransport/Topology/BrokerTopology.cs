namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>Defines the operations required by broker topology.</summary>
public interface BrokerTopology :
    IProbeSite
{
    /// <summary>Gets the topics.</summary>
    Topic[] Topics { get; }
    /// <summary>Gets the queues.</summary>
    Queue[] Queues { get; }
    /// <summary>Gets the topic subscriptions.</summary>
    TopicToTopicSubscription[] TopicSubscriptions { get; }
    /// <summary>Gets the queue subscriptions.</summary>
    TopicToQueueSubscription[] QueueSubscriptions { get; }
}
