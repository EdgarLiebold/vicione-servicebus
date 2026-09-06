namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>Describes a SQL transport subscription that routes a topic to a queue.</summary>
public interface TopicToQueueSubscription
{
    /// <summary>Gets the source.</summary>
    Topic Source { get; }

    /// <summary>Gets the destination.</summary>
    Queue Destination { get; }

    /// <summary>Gets the subscription type.</summary>
    SqlSubscriptionType SubscriptionType { get; }

    /// <summary>Gets the routing key.</summary>
    string? RoutingKey { get; }
}
