namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>
/// Describes a SQL transport subscription that routes one topic to another.
/// </summary>
public interface TopicToTopicSubscription
{
    /// <summary>
    /// Gets the source topic.
    /// </summary>
    Topic Source { get; }

    /// <summary>
    /// Gets the destination topic.
    /// </summary>
    Topic Destination { get; }

    /// <summary>
    /// Gets the subscription type value.
    /// </summary>
    SqlSubscriptionType SubscriptionType { get; }

    /// <summary>
    /// Gets the optional routing key used by the subscription.
    /// </summary>
    string? RoutingKey { get; }
}
