using System;

namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>Configures sql queue subscription.</summary>
public class SqlQueueSubscriptionConfigurator :
    SqlTopicSubscriptionConfigurator
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="topicName">The topic name.</param>
    /// <param name="subscriptionType">The runtime subscription type used by the operation.</param>
    /// <param name="autoDeleteOnIdle">The auto delete on idle.</param>
    /// <param name="routingKey">The routing key.</param>
    protected SqlQueueSubscriptionConfigurator(string topicName, SqlSubscriptionType subscriptionType = SqlSubscriptionType.All,
        TimeSpan? autoDeleteOnIdle = null, string? routingKey = null)
        : base(topicName, subscriptionType, routingKey)
    {
    }
}
