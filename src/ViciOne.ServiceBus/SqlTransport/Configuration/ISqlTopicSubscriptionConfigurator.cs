// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
#nullable enable
namespace ViciOne.ServiceBus
{
    /// <summary>
    /// Configures the topic subscription for the receive endpoint
    /// </summary>
    public interface ISqlTopicSubscriptionConfigurator :
        ISqlTopicConfigurator
    {
        SqlSubscriptionType SubscriptionType { set; }

        string? RoutingKey { set; }
    }
}
