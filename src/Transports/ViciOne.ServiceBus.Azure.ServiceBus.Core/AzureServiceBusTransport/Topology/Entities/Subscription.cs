// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.AzureServiceBusTransport.Topology
{
    using Azure.Messaging.ServiceBus.Administration;


    /// <summary>
    /// A subscription, as defined
    /// </summary>
    public interface Subscription
    {
        CreateSubscriptionOptions CreateSubscriptionOptions { get; }

        TopicHandle Topic { get; }

        CreateRuleOptions Rule { get; }

        RuleFilter Filter { get; }
    }
}
