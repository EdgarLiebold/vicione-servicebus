namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>Describes a SQL transport subscription that routes one topic to another.</summary>
public interface TopicToTopicSubscription
{
    /// <summary>Gets the source.</summary>
    Topic Source { get; }

    /// <summary>Gets the destination.</summary>
    Topic Destination { get; }

    /// <summary>Gets the subscription type.</summary>
    SqlSubscriptionType SubscriptionType { get; }

    /// <summary>Gets the routing key.</summary>
    string? RoutingKey { get; }
}
