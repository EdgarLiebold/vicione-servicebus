using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.AzureServiceBusTransport.Topology;

/// <summary>
/// A subscription, as defined
/// </summary>
public interface Subscription
{
    CreateSubscriptionOptions CreateSubscriptionOptions { get; }

    TopicHandle Topic { get; }

    CreateRuleOptions? Rule { get; }

    RuleFilter? Filter { get; }
}
