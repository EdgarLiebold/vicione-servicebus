using System;

#nullable enable
namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>
/// Provides a sql queue subscription configurator implementation.
/// </summary>
public class SqlQueueSubscriptionConfigurator :
    SqlTopicSubscriptionConfigurator
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="topicName">The topic name value.</param>
    /// <param name="subscriptionType">The subscription type value.</param>
    /// <param name="autoDeleteOnIdle">The auto delete on idle value.</param>
    /// <param name="routingKey">The routing key value.</param>
    protected SqlQueueSubscriptionConfigurator(string topicName, SqlSubscriptionType subscriptionType = SqlSubscriptionType.All,
        TimeSpan? autoDeleteOnIdle = null, string? routingKey = null)
        : base(topicName, subscriptionType, routingKey)
    {
    }
}
