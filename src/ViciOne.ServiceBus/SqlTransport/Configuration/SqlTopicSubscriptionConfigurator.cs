namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>
/// Provides a sql topic subscription configurator implementation.
/// </summary>
public abstract class SqlTopicSubscriptionConfigurator :
    SqlTopicConfigurator,
    ISqlTopicSubscriptionConfigurator
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="topicName">The topic name value.</param>
    /// <param name="subscriptionType">The subscription type value.</param>
    /// <param name="routingKey">The routing key value.</param>
    protected SqlTopicSubscriptionConfigurator(string topicName, SqlSubscriptionType subscriptionType = SqlSubscriptionType.All, string? routingKey = null)
        : base(topicName)
    {
        SubscriptionType = subscriptionType;
        RoutingKey = routingKey;
    }

    /// <summary>
    /// Gets or sets the routing key value.
    /// </summary>
    public string? RoutingKey { get; set; }
    /// <summary>
    /// Gets or sets the subscription type value.
    /// </summary>
    public SqlSubscriptionType SubscriptionType { get; set; }
}
