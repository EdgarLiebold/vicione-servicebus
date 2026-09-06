using System;

namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>Builds broker topology components.</summary>
public interface IBrokerTopologyBuilder
{
    /// <summary>Declares a topic.</summary>
    /// <param name="name">The topic name.</param>
    /// <returns>A handle that identifies the declared topic.</returns>
    TopicHandle CreateTopic(string name);

    /// <summary>Creates a subscription from one topic to another.</summary>
    /// <param name="source">The source topic.</param>
    /// <param name="destination">The destination topic.</param>
    /// <param name="subscriptionType">The runtime subscription type used by the operation.</param>
    /// <param name="routingKey">The binding routing key.</param>
    /// <returns>An entity handle used to reference the binding in subsequent calls.</returns>
    TopicSubscriptionHandle CreateTopicSubscription(TopicHandle source, TopicHandle destination,
        SqlSubscriptionType subscriptionType = SqlSubscriptionType.All, string? routingKey = null);

    /// <summary>Declares a queue.</summary>
    /// <param name="name">The name.</param>
    /// <param name="autoDeleteOnIdle">The auto delete on idle.</param>
    /// <param name="maxDeliveryCount">The max delivery count.</param>
    /// <returns>The created queue.</returns>
    QueueHandle CreateQueue(string name, TimeSpan? autoDeleteOnIdle = null, int? maxDeliveryCount = null);

    /// <summary>Creates a subscription from a topic to a queue.</summary>
    /// <param name="topic">The topic.</param>
    /// <param name="queue">The queue.</param>
    /// <param name="subscriptionType">The runtime subscription type used by the operation.</param>
    /// <param name="routingKey">The routing key.</param>
    /// <returns>The created queue subscription.</returns>
    QueueSubscriptionHandle CreateQueueSubscription(TopicHandle topic, QueueHandle queue,
        SqlSubscriptionType subscriptionType = SqlSubscriptionType.All, string? routingKey = null);
}
