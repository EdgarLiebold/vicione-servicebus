using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Provides messaging and administration operations for one Azure Service Bus namespace.</summary>
public interface ConnectionContext :
    PipeContext
{
    /// <summary>Gets the Azure Service Bus namespace URI without an entity path.</summary>
    Uri Endpoint { get; }

    /// <summary>Creates a non-session processor for a queue.</summary>
    /// <param name="settings">The queue and processor settings.</param>
    /// <returns>The configured queue processor.</returns>
    ServiceBusProcessor CreateQueueProcessor(ReceiveSettings settings);
    /// <summary>Creates a session-aware processor for a queue.</summary>
    /// <param name="settings">The queue and session-processor settings.</param>
    /// <returns>The configured queue session processor.</returns>
    ServiceBusSessionProcessor CreateQueueSessionProcessor(ReceiveSettings settings);

    /// <summary>Creates a non-session processor for a topic subscription.</summary>
    /// <param name="settings">The subscription and processor settings.</param>
    /// <returns>The configured subscription processor.</returns>
    ServiceBusProcessor CreateSubscriptionProcessor(SubscriptionSettings settings);
    /// <summary>Creates a session-aware processor for a topic subscription.</summary>
    /// <param name="settings">The subscription and session-processor settings.</param>
    /// <returns>The configured subscription session processor.</returns>
    ServiceBusSessionProcessor CreateSubscriptionSessionProcessor(SubscriptionSettings settings);

    /// <summary>Creates a sender for a queue or topic entity.</summary>
    /// <param name="entityPath">The entity path relative to the namespace.</param>
    /// <returns>The Azure Service Bus sender.</returns>
    ServiceBusSender CreateMessageSender(string entityPath);

    /// <summary>Creates a queue in this connection's namespace.</summary>
    /// <param name="createQueueOptions">The Azure SDK queue-creation options.</param>
    /// <param name="cancellationToken">The token that cancels the administration request.</param>
    /// <returns>A task that produces the created queue properties.</returns>
    Task<QueueProperties> CreateQueueAsync(CreateQueueOptions createQueueOptions, CancellationToken cancellationToken);

    /// <summary>Creates a topic in this connection's namespace.</summary>
    /// <param name="createTopicOptions">The Azure SDK topic-creation options.</param>
    /// <param name="cancellationToken">The token that cancels the administration request.</param>
    /// <returns>A task that produces the created topic properties.</returns>
    Task<TopicProperties> CreateTopicAsync(CreateTopicOptions createTopicOptions, CancellationToken cancellationToken);

    /// <summary>Creates a topic subscription and its configured initial rule.</summary>
    /// <param name="createSubscriptionOptions">The Azure SDK subscription-creation options.</param>
    /// <param name="rule">An optional complete initial rule.</param>
    /// <param name="filter">An optional filter for the default initial rule.</param>
    /// <param name="cancellationToken">The token that cancels the administration requests.</param>
    /// <returns>A task that produces the created subscription properties.</returns>
    Task<SubscriptionProperties> CreateTopicSubscriptionAsync(CreateSubscriptionOptions createSubscriptionOptions, CreateRuleOptions? rule, RuleFilter? filter,
        CancellationToken cancellationToken);

    /// <summary>Deletes the configured subscription from its topic.</summary>
    /// <param name="subscriptionOptions">Options identifying the topic and subscription.</param>
    /// <param name="cancellationToken">The token that cancels the administration request.</param>
    /// <returns>A task that completes when Azure Service Bus accepts the deletion.</returns>
    Task DeleteTopicSubscriptionAsync(CreateSubscriptionOptions subscriptionOptions, CancellationToken cancellationToken);
}
