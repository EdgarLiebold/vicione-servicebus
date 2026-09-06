using System;
using System.Collections.Generic;
using Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.AzureServiceBus.Topology;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>Builds and validates the Azure Service Bus creation options for a topic subscription.</summary>
public class ServiceBusSubscriptionConfigurator :
    ServiceBusEndpointEntityConfigurator,
    IServiceBusSubscriptionConfigurator
{
    /// <summary>Initializes configuration for a named subscription on a topic.</summary>
    /// <param name="subscriptionName">The subscription name.</param>
    /// <param name="topicPath">The topic path.</param>
    public ServiceBusSubscriptionConfigurator(string subscriptionName, string topicPath)
    {
        TopicPath = topicPath;
        SubscriptionName = subscriptionName;
    }

    /// <summary>Gets or sets whether filter evaluation failures are dead-lettered.</summary>
    public bool? EnableDeadLetteringOnFilterEvaluationExceptions { private get; set; }

    /// <summary>Gets or sets the filter for the subscription's default rule.</summary>
    public RuleFilter? Filter { get; set; }
    /// <summary>Gets or sets the complete rule created with the subscription.</summary>
    public CreateRuleOptions? Rule { get; set; }

    /// <summary>Gets or sets the entity path to which active messages are forwarded.</summary>
    public string? ForwardTo { private get; set; }

    /// <summary>Gets the source topic path.</summary>
    public string TopicPath { get; }

    /// <summary>Gets the subscription name beneath the topic.</summary>
    public string SubscriptionName { get; }

    /// <summary>Validates entity names, idle deletion, and mutually exclusive rule configuration.</summary>
    /// <returns>The subscription configuration failures.</returns>
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

    /// <summary>Projects the configured values into Azure SDK subscription-creation options.</summary>
    /// <returns>The SDK options for creating or comparing the subscription.</returns>
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
