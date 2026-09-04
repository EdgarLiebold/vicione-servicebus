using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Provides a service bus connection context implementation.
/// </summary>
public class ServiceBusConnectionContext :
    BasePipeContext,
    ConnectionContext,
    IAsyncDisposable
{
    readonly ServiceBusAdministrationClient _administrationClient;
    readonly ServiceBusClient _client;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="client">The client value.</param>
    /// <param name="administrationClient">The administration client value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public ServiceBusConnectionContext(ServiceBusClient client, ServiceBusAdministrationClient administrationClient, CancellationToken cancellationToken)
        : base(cancellationToken)
    {
        _client = client;
        _administrationClient = administrationClient;
        Endpoint = new Uri($"sb://{_client.FullyQualifiedNamespace}");
    }

    /// <summary>
    /// Gets the endpoint value.
    /// </summary>
    public Uri Endpoint { get; }

    /// <summary>
    /// Creates queue processor.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    /// <returns>The result of the operation.</returns>
    public ServiceBusProcessor CreateQueueProcessor(ReceiveSettings settings)
    {
        return _client.CreateProcessor(settings.Path, GetProcessorOptions(settings));
    }

    /// <summary>
    /// Creates queue session processor.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    /// <returns>The result of the operation.</returns>
    public ServiceBusSessionProcessor CreateQueueSessionProcessor(ReceiveSettings settings)
    {
        return _client.CreateSessionProcessor(settings.Path, GetSessionProcessorOptions(settings));
    }

    /// <summary>
    /// Creates subscription processor.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    /// <returns>The result of the operation.</returns>
    public ServiceBusProcessor CreateSubscriptionProcessor(SubscriptionSettings settings)
    {
        return _client.CreateProcessor(settings.CreateTopicOptions.Name, settings.CreateSubscriptionOptions.SubscriptionName,
            GetProcessorOptions(settings));
    }

    /// <summary>
    /// Creates subscription session processor.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    /// <returns>The result of the operation.</returns>
    public ServiceBusSessionProcessor CreateSubscriptionSessionProcessor(SubscriptionSettings settings)
    {
        return _client.CreateSessionProcessor(settings.CreateTopicOptions.Name, settings.CreateSubscriptionOptions.SubscriptionName,
            GetSessionProcessorOptions(settings));
    }

    /// <summary>
    /// Creates message sender.
    /// </summary>
    /// <param name="entityPath">The entity path value.</param>
    /// <returns>The result of the operation.</returns>
    public ServiceBusSender CreateMessageSender(string entityPath)
    {
        return _client.CreateSender(entityPath);
    }

    /// <summary>
    /// Creates queue.
    /// </summary>
    /// <param name="createQueueOptions">The create queue options value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<QueueProperties> CreateQueueAsync(CreateQueueOptions createQueueOptions, CancellationToken cancellationToken)
    {
        QueueProperties? queueProperties = null;
        try
        {
            queueProperties = await GetQueueAsync(createQueueOptions.Name, cancellationToken).ConfigureAwait(false);
        }
        catch (ServiceBusException exception) when (exception.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
        {
        }

        if (queueProperties == null)
        {
            try
            {
                TransportLogMessages.CreateQueue(createQueueOptions.Name);

                queueProperties = await CreateQueueCoreAsync(createQueueOptions, cancellationToken).ConfigureAwait(false);
            }
            catch (ServiceBusException sbe) when (sbe.Reason == ServiceBusFailureReason.MessagingEntityAlreadyExists)
            {
                queueProperties = await GetQueueAsync(createQueueOptions.Name, cancellationToken).ConfigureAwait(false);
            }
        }

        LogContext.Debug?.Log("Queue: {Queue} ({Attributes})", createQueueOptions.Name,
            string.Join(", ",
                new[]
                {
                    createQueueOptions.RequiresDuplicateDetection ? "dupe detect" : "",
                    createQueueOptions.DeadLetteringOnMessageExpiration ? "dead letter" : "",
                    createQueueOptions.RequiresSession ? "session" : "",
                    createQueueOptions.AutoDeleteOnIdle != Defaults.AutoDeleteOnIdle
                        ? $"auto-delete: {createQueueOptions.AutoDeleteOnIdle.ToFriendlyString()}"
                        : ""
                }.Where(x => !string.IsNullOrWhiteSpace(x))));

        return queueProperties;
    }

    /// <summary>
    /// Creates topic.
    /// </summary>
    /// <param name="createTopicOptions">The create topic options value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<TopicProperties> CreateTopicAsync(CreateTopicOptions createTopicOptions, CancellationToken cancellationToken)
    {
        TopicProperties? topicProperties = null;
        try
        {
            topicProperties = await GetTopicAsync(createTopicOptions.Name, cancellationToken).ConfigureAwait(false);
        }
        catch (ServiceBusException exception) when (exception.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
        {
        }

        if (topicProperties == null)
        {
            try
            {
                TransportLogMessages.CreateTopic(createTopicOptions.Name);

                topicProperties = await CreateTopicCoreAsync(createTopicOptions, cancellationToken).ConfigureAwait(false);
            }
            catch (ServiceBusException e) when (e.Reason == ServiceBusFailureReason.MessagingEntityAlreadyExists)
            {
                topicProperties = await GetTopicAsync(createTopicOptions.Name, cancellationToken).ConfigureAwait(false);
            }
        }

        LogContext.Debug?.Log("Topic: {Topic} ({Attributes})", createTopicOptions.Name,
            string.Join(", ",
                new[]
                {
                    createTopicOptions.RequiresDuplicateDetection ? "dupe detect" : "",
                    createTopicOptions.EnablePartitioning ? "partitioned" : "",
                    createTopicOptions.SupportOrdering ? "ordered" : ""
                }.Where(x => !string.IsNullOrWhiteSpace(x))));

        return topicProperties;
    }

    /// <summary>
    /// Creates topic subscription.
    /// </summary>
    /// <param name="createSubscriptionOptions">The create subscription options value.</param>
    /// <param name="rule">The rule value.</param>
    /// <param name="filter">The filter value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<SubscriptionProperties> CreateTopicSubscriptionAsync(CreateSubscriptionOptions createSubscriptionOptions, CreateRuleOptions? rule,
        RuleFilter? filter, CancellationToken cancellationToken)
    {
        var create = true;
        SubscriptionProperties? subscriptionProperties = null;
        try
        {
            subscriptionProperties = await GetSubscriptionAsync(createSubscriptionOptions.TopicName, createSubscriptionOptions.SubscriptionName, cancellationToken)
                .ConfigureAwait(false);

            string NormalizeForwardTo(string? forwardTo)
            {
                return string.IsNullOrEmpty(forwardTo)
                    ? string.Empty
                    : Uri.IsWellFormedUriString(forwardTo, UriKind.Absolute)
                        ? new Uri(forwardTo).AbsolutePath.TrimStart('/')
                        : forwardTo.Replace(Endpoint.ToString(), string.Empty).Trim('/');
            }

            var targetForwardTo = NormalizeForwardTo(createSubscriptionOptions.ForwardTo);
            var currentForwardTo = NormalizeForwardTo(subscriptionProperties.ForwardTo);

            if (!targetForwardTo.Equals(currentForwardTo)
                || createSubscriptionOptions.LockDuration != subscriptionProperties.LockDuration
                || createSubscriptionOptions.MaxDeliveryCount != subscriptionProperties.MaxDeliveryCount
                || createSubscriptionOptions.EnableBatchedOperations != subscriptionProperties.EnableBatchedOperations
                || createSubscriptionOptions.DeadLetteringOnMessageExpiration != subscriptionProperties.DeadLetteringOnMessageExpiration)
            {
                LogContext.Debug?.Log("Updating subscription: {Subscription} ({Topic} -> {ForwardTo})", subscriptionProperties.SubscriptionName,
                    createSubscriptionOptions.TopicName, createSubscriptionOptions.ForwardTo);

                subscriptionProperties.ForwardTo = createSubscriptionOptions.ForwardTo;
                subscriptionProperties.LockDuration = createSubscriptionOptions.LockDuration;
                subscriptionProperties.MaxDeliveryCount = createSubscriptionOptions.MaxDeliveryCount;
                subscriptionProperties.EnableBatchedOperations = createSubscriptionOptions.EnableBatchedOperations;
                subscriptionProperties.DeadLetteringOnMessageExpiration = createSubscriptionOptions.DeadLetteringOnMessageExpiration;

                await UpdateSubscriptionAsync(subscriptionProperties, cancellationToken).ConfigureAwait(false);
            }

            if (rule != null)
            {
                var ruleProperties = await GetRuleAsync(createSubscriptionOptions.TopicName, createSubscriptionOptions.SubscriptionName, rule.Name, cancellationToken)
                    .ConfigureAwait(false);
                if (rule.Name == ruleProperties.Name && (!(rule.Filter?.Equals(ruleProperties.Filter) ?? ruleProperties.Filter == null)
                        || !(rule.Action?.Equals(ruleProperties.Action) ?? ruleProperties.Action == null)))
                {
                    LogContext.Debug?.Log("Updating subscription Rule: {Rule} ({DescriptionFilter} -> {Filter})", rule.Name,
                        ruleProperties.Filter?.ToString(), rule.Filter?.ToString());

                    ruleProperties.Filter = rule.Filter;
                    ruleProperties.Action = rule.Action;

                    await UpdateRuleAsync(createSubscriptionOptions.TopicName, createSubscriptionOptions.SubscriptionName, ruleProperties, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            else if (filter != null)
            {
                IList<RuleProperties> rules = await GetRulesAsync(createSubscriptionOptions.TopicName, createSubscriptionOptions.SubscriptionName, cancellationToken)
                    .ConfigureAwait(false);
                if (rules.Count == 1)
                {
                    var existingRule = rules[0];

                    if (Guid.TryParse(existingRule.Name, out _) && !(existingRule.Filter?.Equals(filter) ?? filter == null))
                    {
                        LogContext.Debug?.Log("Updating subscription filter: {Rule} ({DescriptionFilter} -> {Filter})", existingRule.Name,
                            existingRule.Filter?.ToString(), filter?.ToString());

                        existingRule.Filter = filter;

                        await UpdateRuleAsync(createSubscriptionOptions.TopicName, createSubscriptionOptions.SubscriptionName, existingRule, cancellationToken)
                            .ConfigureAwait(false);
                    }
                }
            }

            create = false;
        }
        catch (ServiceBusException e) when (e.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
        {
        }

        if (create)
        {
            var created = false;
            try
            {
                LogContext.Debug?.Log("Creating subscription {Subscription} {Topic} -> {ForwardTo}", createSubscriptionOptions.SubscriptionName,
                    createSubscriptionOptions.TopicName,
                    createSubscriptionOptions.ForwardTo);

                subscriptionProperties = rule != null
                    ? await CreateSubscriptionAsync(createSubscriptionOptions, rule, cancellationToken).ConfigureAwait(false)
                    : filter != null
                        ? await CreateSubscriptionAsync(createSubscriptionOptions, filter, cancellationToken).ConfigureAwait(false)
                        : await CreateSubscriptionAsync(createSubscriptionOptions, cancellationToken).ConfigureAwait(false);

                created = true;
            }
            catch (ServiceBusException e) when (e.Reason == ServiceBusFailureReason.MessagingEntityAlreadyExists)
            {
            }

            if (!created)
            {
                subscriptionProperties = await GetSubscriptionAsync(createSubscriptionOptions.TopicName, createSubscriptionOptions.SubscriptionName, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        if (subscriptionProperties == null)
        {
            throw new InvalidOperationException(
                $"Azure Service Bus did not return subscription '{createSubscriptionOptions.SubscriptionName}' on topic '{createSubscriptionOptions.TopicName}'.");
        }

        LogContext.Debug?.Log("Subscription {Subscription} ({Topic} -> {ForwardTo})", subscriptionProperties.SubscriptionName,
            subscriptionProperties.TopicName, subscriptionProperties.ForwardTo);

        return subscriptionProperties;
    }

    /// <summary>
    /// Performs the delete topic subscription operation.
    /// </summary>
    /// <param name="subscriptionOptions">The subscription options value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task DeleteTopicSubscriptionAsync(CreateSubscriptionOptions subscriptionOptions, CancellationToken cancellationToken)
    {
        try
        {
            await DeleteSubscriptionAsync(subscriptionOptions, cancellationToken).ConfigureAwait(false);

            LogContext.Debug?.Log("Subscription Deleted: {Subscription} {Topic}", subscriptionOptions.SubscriptionName, subscriptionOptions.TopicName);
        }
        catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
        {
        }
        catch (Exception ex)
        {
            LogContext.Error?.Log(ex, "Subscription Delete Faulted: {Subscription} {Topic}", subscriptionOptions.SubscriptionName,
                subscriptionOptions.TopicName);
        }
    }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public async ValueTask DisposeAsync()
    {
        var address = _client.FullyQualifiedNamespace;

        TransportLogMessages.DisconnectHost(address);

        await _client.DisposeAsync().ConfigureAwait(false);

        TransportLogMessages.DisconnectedHost(address);
    }

    static ServiceBusSessionProcessorOptions GetSessionProcessorOptions(ClientSettings settings)
    {
        return new ServiceBusSessionProcessorOptions
        {
            AutoCompleteMessages = false,
            PrefetchCount = settings.PrefetchCount,
            MaxAutoLockRenewalDuration = settings.MaxAutoRenewDuration,
            ReceiveMode = ServiceBusReceiveMode.PeekLock,
            MaxConcurrentSessions = settings.MaxConcurrentSessions,
            MaxConcurrentCallsPerSession = settings.MaxConcurrentCallsPerSession,
            SessionIdleTimeout = settings.SessionIdleTimeout
        };
    }

    static ServiceBusProcessorOptions GetProcessorOptions(ClientSettings settings)
    {
        return new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            PrefetchCount = settings.PrefetchCount,
            MaxAutoLockRenewalDuration = settings.MaxAutoRenewDuration,
            ReceiveMode = ServiceBusReceiveMode.PeekLock,
            MaxConcurrentCalls = settings.MaxConcurrentCalls,
        };
    }

    async Task<QueueProperties> GetQueueAsync(string path, CancellationToken cancellationToken)
    {
        return await _administrationClient.GetQueueAsync(path, cancellationToken).ConfigureAwait(false);
    }

    async Task<QueueProperties> CreateQueueCoreAsync(CreateQueueOptions createQueueOptions, CancellationToken cancellationToken)
    {
        return await _administrationClient.CreateQueueAsync(createQueueOptions, cancellationToken).ConfigureAwait(false);
    }

    async Task<TopicProperties> GetTopicAsync(string path, CancellationToken cancellationToken)
    {
        return await _administrationClient.GetTopicAsync(path, cancellationToken).ConfigureAwait(false);
    }

    async Task<TopicProperties> CreateTopicCoreAsync(CreateTopicOptions createTopicOptions, CancellationToken cancellationToken)
    {
        return await _administrationClient.CreateTopicAsync(createTopicOptions, cancellationToken).ConfigureAwait(false);
    }

    async Task<SubscriptionProperties> GetSubscriptionAsync(string topicPath, string subscriptionName, CancellationToken cancellationToken)
    {
        return await _administrationClient.GetSubscriptionAsync(topicPath, subscriptionName, cancellationToken).ConfigureAwait(false);
    }

    async Task DeleteSubscriptionAsync(CreateSubscriptionOptions subscriptionOptions, CancellationToken cancellationToken)
    {
        await _administrationClient.DeleteSubscriptionAsync(subscriptionOptions.TopicName, subscriptionOptions.SubscriptionName, cancellationToken)
            .ConfigureAwait(false);
    }

    async Task<SubscriptionProperties> UpdateSubscriptionAsync(SubscriptionProperties subscriptionProperties, CancellationToken cancellationToken)
    {
        return await _administrationClient.UpdateSubscriptionAsync(subscriptionProperties, cancellationToken).ConfigureAwait(false);
    }

    async Task<RuleProperties> GetRuleAsync(string topicPath, string subscriptionName, string ruleName, CancellationToken cancellationToken)
    {
        return await _administrationClient.GetRuleAsync(topicPath, subscriptionName, ruleName, cancellationToken).ConfigureAwait(false);
    }

    async Task<IList<RuleProperties>> GetRulesAsync(string topicPath, string subscriptionName, CancellationToken cancellationToken)
    {
        return await _administrationClient.GetRulesAsync(topicPath, subscriptionName, cancellationToken).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    async Task<RuleProperties> UpdateRuleAsync(string topicPath, string subscriptionName, RuleProperties ruleProperties, CancellationToken cancellationToken)
    {
        return await _administrationClient.UpdateRuleAsync(topicPath, subscriptionName, ruleProperties, cancellationToken).ConfigureAwait(false);
    }

    async Task<SubscriptionProperties> CreateSubscriptionAsync(CreateSubscriptionOptions createSubscriptionOptions, CreateRuleOptions rule, CancellationToken cancellationToken)
    {
        return await _administrationClient.CreateSubscriptionAsync(createSubscriptionOptions, rule, cancellationToken).ConfigureAwait(false);
    }

    async Task<SubscriptionProperties> CreateSubscriptionAsync(CreateSubscriptionOptions createSubscriptionOptions, RuleFilter filter, CancellationToken cancellationToken)
    {
        var createRuleOptions = new CreateRuleOptions(NewId.NextGuid().ToString(), filter);

        return await _administrationClient.CreateSubscriptionAsync(createSubscriptionOptions, createRuleOptions, cancellationToken).ConfigureAwait(false);
    }

    async Task<SubscriptionProperties> CreateSubscriptionAsync(CreateSubscriptionOptions createSubscriptionOptions, CancellationToken cancellationToken)
    {
        return await _administrationClient.CreateSubscriptionAsync(createSubscriptionOptions, cancellationToken).ConfigureAwait(false);
    }
}
