using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Describes an Azure Service Bus subscription receive endpoint and its broker declarations.</summary>
public interface SubscriptionSettings :
    ClientSettings
{
    /// <summary>Gets the parent topic declaration options.</summary>
    CreateTopicOptions CreateTopicOptions { get; }

    /// <summary>Gets the subscription declaration options.</summary>
    CreateSubscriptionOptions CreateSubscriptionOptions { get; }

    /// <summary>Gets the optional initial subscription rule.</summary>
    CreateRuleOptions? Rule { get; }

    /// <summary>Gets the optional filter for the initial subscription rule.</summary>
    RuleFilter? Filter { get; }
}
