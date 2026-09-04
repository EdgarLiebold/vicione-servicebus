using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Defines the contract for subscription settings.
/// </summary>
public interface SubscriptionSettings :
    ClientSettings
{
    /// <summary>
    /// Gets the create topic options value.
    /// </summary>
    CreateTopicOptions CreateTopicOptions { get; }

    /// <summary>
    /// Gets the create subscription options value.
    /// </summary>
    CreateSubscriptionOptions CreateSubscriptionOptions { get; }

    /// <summary>
    /// Gets the rule value.
    /// </summary>
    CreateRuleOptions? Rule { get; }

    /// <summary>
    /// Gets the filter value.
    /// </summary>
    RuleFilter? Filter { get; }
}
