using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.AzureServiceBusTransport;

public interface SubscriptionSettings :
    ClientSettings
{
    CreateTopicOptions CreateTopicOptions { get; }

    CreateSubscriptionOptions CreateSubscriptionOptions { get; }

    CreateRuleOptions? Rule { get; }

    RuleFilter? Filter { get; }
}
