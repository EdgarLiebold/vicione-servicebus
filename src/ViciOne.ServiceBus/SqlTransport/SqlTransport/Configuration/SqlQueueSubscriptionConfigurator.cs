// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
#nullable enable
namespace ViciOne.ServiceBus.SqlTransport.Configuration
{
    using System;


    public class SqlQueueSubscriptionConfigurator :
        SqlTopicSubscriptionConfigurator
    {
        protected SqlQueueSubscriptionConfigurator(string topicName, SqlSubscriptionType subscriptionType = SqlSubscriptionType.All,
            TimeSpan? autoDeleteOnIdle = null, string? routingKey = null)
            : base(topicName, subscriptionType, routingKey)
        {
        }
    }
}
