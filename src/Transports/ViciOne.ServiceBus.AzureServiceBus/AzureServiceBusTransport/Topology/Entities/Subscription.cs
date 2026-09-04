using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>
/// A subscription, as defined
/// </summary>
public interface Subscription
{
    /// <summary>
    /// Gets the create subscription options value.
    /// </summary>
    CreateSubscriptionOptions CreateSubscriptionOptions { get; }

    /// <summary>
    /// Gets the topic value.
    /// </summary>
    TopicHandle Topic { get; }

    /// <summary>
    /// Gets the rule value.
    /// </summary>
    CreateRuleOptions? Rule { get; }

    /// <summary>
    /// Gets the filter value.
    /// </summary>
    RuleFilter? Filter { get; }
}
