using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Service Bus Connection Context
/// </summary>
public interface ConnectionContext :
    PipeContext
{
    /// <summary>
    /// The Azure Service Bus endpoint, which is a Uri, but without any path information.
    /// </summary>
    Uri Endpoint { get; }

    /// <summary>
    /// Creates queue processor.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    /// <returns>The result of the operation.</returns>
    ServiceBusProcessor CreateQueueProcessor(ReceiveSettings settings);
    /// <summary>
    /// Creates queue session processor.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    /// <returns>The result of the operation.</returns>
    ServiceBusSessionProcessor CreateQueueSessionProcessor(ReceiveSettings settings);

    /// <summary>
    /// Creates subscription processor.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    /// <returns>The result of the operation.</returns>
    ServiceBusProcessor CreateSubscriptionProcessor(SubscriptionSettings settings);
    /// <summary>
    /// Creates subscription session processor.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    /// <returns>The result of the operation.</returns>
    ServiceBusSessionProcessor CreateSubscriptionSessionProcessor(SubscriptionSettings settings);

    /// <summary>
    /// Creates message sender.
    /// </summary>
    /// <param name="entityPath">The entity path value.</param>
    /// <returns>The result of the operation.</returns>
    ServiceBusSender CreateMessageSender(string entityPath);

    /// <summary>
    /// Create a queue in the host namespace (which is scoped to the full ServiceUri)
    /// </summary>
    /// <param name="createQueueOptions"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<QueueProperties> CreateQueueAsync(CreateQueueOptions createQueueOptions, CancellationToken cancellationToken);

    /// <summary>
    /// Create a topic in the root namespace
    /// </summary>
    /// <param name="createTopicOptions"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<TopicProperties> CreateTopicAsync(CreateTopicOptions createTopicOptions, CancellationToken cancellationToken);

    /// <summary>
    /// Create a topic subscription
    /// </summary>
    /// <param name="createSubscriptionOptions"></param>
    /// <param name="rule"></param>
    /// <param name="filter"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<SubscriptionProperties> CreateTopicSubscriptionAsync(CreateSubscriptionOptions createSubscriptionOptions, CreateRuleOptions? rule, RuleFilter? filter,
        CancellationToken cancellationToken);

    /// <summary>
    /// Delete a subscription from the topic
    /// </summary>
    /// <param name="subscriptionOptions"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task DeleteTopicSubscriptionAsync(CreateSubscriptionOptions subscriptionOptions, CancellationToken cancellationToken);
}
