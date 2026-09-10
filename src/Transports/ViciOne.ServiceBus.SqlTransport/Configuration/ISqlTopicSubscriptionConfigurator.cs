namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Configures the topic subscription for the receive endpoint.</summary>
public interface ISqlTopicSubscriptionConfigurator :
    ISqlTopicConfigurator
{
    /// <summary>Gets or sets the subscription type.</summary>
    SqlSubscriptionType SubscriptionType { set; }

    /// <summary>Gets or sets the routing key.</summary>
    string? RoutingKey { set; }
}
