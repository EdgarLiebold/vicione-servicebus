namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>Configures sql topic subscription.</summary>
public abstract class SqlTopicSubscriptionConfigurator :
    SqlTopicConfigurator,
    ISqlTopicSubscriptionConfigurator
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="topicName">The topic name.</param>
    /// <param name="subscriptionType">The runtime subscription type used by the operation.</param>
    /// <param name="routingKey">The routing key.</param>
    protected SqlTopicSubscriptionConfigurator(string topicName, SqlSubscriptionType subscriptionType = SqlSubscriptionType.All, string? routingKey = null)
        : base(topicName)
    {
        SubscriptionType = subscriptionType;
        RoutingKey = routingKey;
    }

    /// <summary>Gets or sets the routing key.</summary>
    public string? RoutingKey { get; set; }
    /// <summary>Gets or sets the subscription type.</summary>
    public SqlSubscriptionType SubscriptionType { get; set; }
}
