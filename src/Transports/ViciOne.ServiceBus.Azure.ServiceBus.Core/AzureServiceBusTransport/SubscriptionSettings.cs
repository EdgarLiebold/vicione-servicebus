// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.AzureServiceBusTransport
{
    using Azure.Messaging.ServiceBus.Administration;


    public interface SubscriptionSettings :
        ClientSettings
    {
        CreateTopicOptions CreateTopicOptions { get; }

        CreateSubscriptionOptions CreateSubscriptionOptions { get; }

        CreateRuleOptions Rule { get; }

        RuleFilter Filter { get; }
    }
}
