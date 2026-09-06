namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>
/// Configures the topic subscription for the receive endpoint
/// </summary>
public interface ISqlTopicSubscriptionConfigurator :
    ISqlTopicConfigurator
{
    /// <summary>
    /// Gets or sets the subscription type value.
    /// </summary>
    SqlSubscriptionType SubscriptionType { set; }

    /// <summary>
    /// Gets or sets the routing key value.
    /// </summary>
    string? RoutingKey { set; }
}
