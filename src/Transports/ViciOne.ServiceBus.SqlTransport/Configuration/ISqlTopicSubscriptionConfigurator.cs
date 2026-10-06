namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Configures the topic subscription for the receive endpoint.</summary>
public interface ISqlTopicSubscriptionConfigurator
{
    /// <summary>Sets the topic subscription mode.</summary>
    SqlSubscriptionType SubscriptionType { set; }

    /// <summary>Sets the routing key for the topic subscription.</summary>
    string? RoutingKey { set; }
}
