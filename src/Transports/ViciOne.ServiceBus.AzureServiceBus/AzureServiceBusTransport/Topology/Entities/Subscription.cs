using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>Describes an Azure Service Bus subscription declaration and its optional initial rule.</summary>
public interface Subscription
{
    /// <summary>Gets the Azure subscription declaration options.</summary>
    CreateSubscriptionOptions CreateSubscriptionOptions { get; }

    /// <summary>Gets the handle of the subscribed topic.</summary>
    TopicHandle Topic { get; }

    /// <summary>Gets the optional initial subscription rule.</summary>
    CreateRuleOptions? Rule { get; }

    /// <summary>Gets the optional filter associated with the initial rule.</summary>
    RuleFilter? Filter { get; }
}
