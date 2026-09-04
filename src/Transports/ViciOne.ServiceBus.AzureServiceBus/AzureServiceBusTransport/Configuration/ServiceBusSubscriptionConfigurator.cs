using System;
using System.Collections.Generic;
using Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.AzureServiceBus.Topology;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>
/// Provides a service bus subscription configurator implementation.
/// </summary>
public class ServiceBusSubscriptionConfigurator :
    ServiceBusEndpointEntityConfigurator,
    IServiceBusSubscriptionConfigurator
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="subscriptionName">The subscription name value.</param>
    /// <param name="topicPath">The topic path value.</param>
    public ServiceBusSubscriptionConfigurator(string subscriptionName, string topicPath)
    {
        TopicPath = topicPath;
        SubscriptionName = subscriptionName;
    }

    /// <summary>
    /// Gets or sets the enable dead lettering on filter evaluation exceptions value.
    /// </summary>
    public bool? EnableDeadLetteringOnFilterEvaluationExceptions { private get; set; }

    /// <summary>
    /// Gets or sets the filter value.
    /// </summary>
    public RuleFilter? Filter { get; set; }
    /// <summary>
    /// Gets or sets the rule value.
    /// </summary>
    public CreateRuleOptions? Rule { get; set; }

    /// <summary>
    /// Gets or sets the forward to value.
    /// </summary>
    public string? ForwardTo { private get; set; }

    /// <summary>
    /// Gets the topic path value.
    /// </summary>
    public string TopicPath { get; }

    /// <summary>
    /// Gets the subscription name value.
    /// </summary>
    public string SubscriptionName { get; }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (!ServiceBusEntityNameValidator.Validator.IsValidEntityName(TopicPath))
            yield return this.Failure("TopicPath", $"must be a valid topic path: {TopicPath}");

        if (!ServiceBusSubscriptionNameValidator.Validator.IsValidEntityName(SubscriptionName))
            yield return this.Failure("SubscriptionName", $"must be a valid subscription name: {SubscriptionName}");

        if (AutoDeleteOnIdle.HasValue && AutoDeleteOnIdle != TimeSpan.Zero && AutoDeleteOnIdle < TimeSpan.FromMinutes(5))
            yield return this.Failure("AutoDeleteOnIdle", "must be zero, or >= 5:00");

        if (Rule != null && Filter != null)
            yield return this.Failure("Rule/Filter", "only a rule or a filter may be specified");
    }

    /// <summary>
    /// Gets create subscription options.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public CreateSubscriptionOptions GetCreateSubscriptionOptions()
    {
        var options = new CreateSubscriptionOptions(TopicPath, SubscriptionName);

        if (AutoDeleteOnIdle.HasValue)
            options.AutoDeleteOnIdle = AutoDeleteOnIdle.Value;

        if (DefaultMessageTimeToLive.HasValue)
            options.DefaultMessageTimeToLive = DefaultMessageTimeToLive.Value;

        if (EnableBatchedOperations.HasValue)
            options.EnableBatchedOperations = EnableBatchedOperations.Value;

        if (EnableDeadLetteringOnFilterEvaluationExceptions.HasValue)
            options.EnableDeadLetteringOnFilterEvaluationExceptions = EnableDeadLetteringOnFilterEvaluationExceptions.Value;

        if (EnableDeadLetteringOnMessageExpiration.HasValue)
            options.DeadLetteringOnMessageExpiration = EnableDeadLetteringOnMessageExpiration.Value;

        if (!string.IsNullOrWhiteSpace(ForwardDeadLetteredMessagesTo))
            options.ForwardDeadLetteredMessagesTo = ForwardDeadLetteredMessagesTo;

        if (!string.IsNullOrWhiteSpace(ForwardTo))
            options.ForwardTo = ForwardTo;

        if (LockDuration.HasValue)
            options.LockDuration = LockDuration.Value;

        if (MaxDeliveryCount.HasValue)
            options.MaxDeliveryCount = MaxDeliveryCount.Value;

        if (RequiresSession.HasValue)
            options.RequiresSession = RequiresSession.Value;

        if (!string.IsNullOrWhiteSpace(UserMetadata))
            options.UserMetadata = UserMetadata;

        return options;
    }
}
