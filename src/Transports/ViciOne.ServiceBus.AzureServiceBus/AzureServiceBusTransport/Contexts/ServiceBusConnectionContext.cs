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

/// <summary>Wraps Azure Service Bus messaging and administration clients for one namespace.</summary>
public class ServiceBusConnectionContext :
    BasePipeContext,
    ConnectionContext,
    IAsyncDisposable
{
    readonly ServiceBusAdministrationClient _administrationClient;
    readonly ServiceBusClient _client;
    readonly bool _ownsClient;

    /// <summary>Initializes the namespace context from messaging and administration clients.</summary>
    /// <param name="client">The client used for senders and processors.</param>
    /// <param name="administrationClient">The client used to create, inspect, update, and delete entities.</param>
    /// <param name="cancellationToken">The token that ends the context lifetime.</param>
    /// <remarks>This constructor transfers ownership of the messaging client to the context. Host configuration with caller-owned clients retains their external ownership.</remarks>
    public ServiceBusConnectionContext(ServiceBusClient client, ServiceBusAdministrationClient administrationClient, CancellationToken cancellationToken)
        : base(cancellationToken)
    {
        _client = client;
        _administrationClient = administrationClient;
        _ownsClient = true;
        Endpoint = new Uri($"sb://{_client.FullyQualifiedNamespace}");
    }

    internal ServiceBusConnectionContext(ServiceBusClient client, ServiceBusAdministrationClient administrationClient,
        CancellationToken cancellationToken, Uri endpoint, bool ownsClient)
        : this(client, administrationClient, cancellationToken)
    {
        Endpoint = endpoint;
        _ownsClient = ownsClient;
    }

    /// <summary>Gets the namespace URI used to form entity addresses.</summary>
    public Uri Endpoint { get; }

    /// <summary>Creates a non-session processor for a queue.</summary>
    /// <param name="settings">The queue and processor settings.</param>
    /// <returns>The configured queue processor.</returns>
    public ServiceBusProcessor CreateQueueProcessor(ReceiveSettings settings)
    {
        return _client.CreateProcessor(settings.Path, GetProcessorOptions(settings));
    }

    /// <summary>Creates a session-aware processor for a queue.</summary>
    /// <param name="settings">The queue and session-processor settings.</param>
    /// <returns>The configured queue session processor.</returns>
    public ServiceBusSessionProcessor CreateQueueSessionProcessor(ReceiveSettings settings)
    {
        return _client.CreateSessionProcessor(settings.Path, GetSessionProcessorOptions(settings));
    }

    /// <summary>Creates a non-session processor for a topic subscription.</summary>
    /// <param name="settings">The topic, subscription, and processor settings.</param>
    /// <returns>The configured subscription processor.</returns>
    public ServiceBusProcessor CreateSubscriptionProcessor(SubscriptionSettings settings)
    {
        return _client.CreateProcessor(settings.CreateTopicOptions.Name, settings.CreateSubscriptionOptions.SubscriptionName,
            GetProcessorOptions(settings));
    }

    /// <summary>Creates a session-aware processor for a topic subscription.</summary>
    /// <param name="settings">The topic, subscription, and session-processor settings.</param>
    /// <returns>The configured subscription session processor.</returns>
    public ServiceBusSessionProcessor CreateSubscriptionSessionProcessor(SubscriptionSettings settings)
    {
        return _client.CreateSessionProcessor(settings.CreateTopicOptions.Name, settings.CreateSubscriptionOptions.SubscriptionName,
            GetSessionProcessorOptions(settings));
    }

    /// <summary>Creates a sender for a queue or topic entity.</summary>
    /// <param name="entityPath">The entity path relative to the namespace.</param>
    /// <returns>The Azure Service Bus sender.</returns>
    public ServiceBusSender CreateMessageSender(string entityPath)
    {
        return _client.CreateSender(entityPath);
    }

    /// <summary>Returns an existing queue or creates it atomically when it is absent.</summary>
    /// <param name="createQueueOptions">The desired queue properties.</param>
    /// <param name="cancellationToken">The token that cancels administration requests.</param>
    /// <returns>A task that produces the existing or newly created queue properties.</returns>
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

    /// <summary>Returns an existing topic or creates it atomically when it is absent.</summary>
    /// <param name="createTopicOptions">The desired topic properties.</param>
    /// <param name="cancellationToken">The token that cancels administration requests.</param>
    /// <returns>A task that produces the existing or newly created topic properties.</returns>
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

    /// <summary>Creates a subscription when absent, or reconciles its forwarding, delivery, and initial-rule settings when present.</summary>
    /// <param name="createSubscriptionOptions">The desired subscription properties.</param>
    /// <param name="rule">An optional complete initial rule.</param>
    /// <param name="filter">An optional filter for the generated initial rule.</param>
    /// <param name="cancellationToken">The token that cancels administration requests.</param>
    /// <returns>A task that produces the reconciled subscription properties.</returns>
    public async Task<SubscriptionProperties> CreateTopicSubscriptionAsync(CreateSubscriptionOptions createSubscriptionOptions, CreateRuleOptions? rule,
        RuleFilter? filter, CancellationToken cancellationToken)
    {
        var create = true;
        SubscriptionProperties? subscriptionProperties = null;
        try
        {
            subscriptionProperties = await GetSubscriptionAsync(createSubscriptionOptions.TopicName, createSubscriptionOptions.SubscriptionName, cancellationToken)
                .ConfigureAwait(false);

            await ReconcileSubscriptionAsync(createSubscriptionOptions, subscriptionProperties, rule, filter, cancellationToken).ConfigureAwait(false);

            create = false;
        }
        catch (ServiceBusException e) when (subscriptionProperties == null && e.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
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
                await ReconcileSubscriptionAsync(createSubscriptionOptions, subscriptionProperties, rule, filter, cancellationToken).ConfigureAwait(false);
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

    async Task ReconcileSubscriptionAsync(CreateSubscriptionOptions options, SubscriptionProperties properties,
        CreateRuleOptions? rule, RuleFilter? filter, CancellationToken cancellationToken)
    {
        if (SubscriptionSettingsDiffer(options, properties))
        {
            LogContext.Debug?.Log("Updating subscription: {Subscription} ({Topic} -> {ForwardTo})", properties.SubscriptionName,
                options.TopicName, options.ForwardTo);

            properties.ForwardTo = options.ForwardTo;
            properties.LockDuration = options.LockDuration;
            properties.MaxDeliveryCount = options.MaxDeliveryCount;
            properties.EnableBatchedOperations = options.EnableBatchedOperations;
            properties.DeadLetteringOnMessageExpiration = options.DeadLetteringOnMessageExpiration;

            await UpdateSubscriptionAsync(properties, cancellationToken).ConfigureAwait(false);
        }

        if (rule != null)
            await ReconcileNamedRuleAsync(options, rule, cancellationToken).ConfigureAwait(false);
        else if (filter != null)
            await ReconcileGeneratedFilterAsync(options, filter, cancellationToken).ConfigureAwait(false);
    }

    bool SubscriptionSettingsDiffer(CreateSubscriptionOptions options, SubscriptionProperties properties)
    {
        return !NormalizeForwardTo(options.ForwardTo).Equals(NormalizeForwardTo(properties.ForwardTo))
            || options.LockDuration != properties.LockDuration
            || options.MaxDeliveryCount != properties.MaxDeliveryCount
            || options.EnableBatchedOperations != properties.EnableBatchedOperations
            || options.DeadLetteringOnMessageExpiration != properties.DeadLetteringOnMessageExpiration;
    }

    string NormalizeForwardTo(string? forwardTo)
    {
        return string.IsNullOrEmpty(forwardTo)
            ? string.Empty
            : Uri.IsWellFormedUriString(forwardTo, UriKind.Absolute)
                ? new Uri(forwardTo).AbsolutePath.TrimStart('/')
                : forwardTo.Replace(Endpoint.ToString(), string.Empty).Trim('/');
    }

    async Task ReconcileNamedRuleAsync(CreateSubscriptionOptions options, CreateRuleOptions rule, CancellationToken cancellationToken)
    {
        var properties = await GetRuleAsync(options.TopicName, options.SubscriptionName, rule.Name, cancellationToken).ConfigureAwait(false);
        if (rule.Name == properties.Name && (!(rule.Filter?.Equals(properties.Filter) ?? properties.Filter == null)
                || !(rule.Action?.Equals(properties.Action) ?? properties.Action == null)))
        {
            LogContext.Debug?.Log("Updating subscription Rule: {Rule} ({DescriptionFilter} -> {Filter})", rule.Name,
                properties.Filter?.ToString(), rule.Filter?.ToString());

            properties.Filter = rule.Filter;
            properties.Action = rule.Action;

            await UpdateRuleAsync(options.TopicName, options.SubscriptionName, properties, cancellationToken).ConfigureAwait(false);
        }
    }

    async Task ReconcileGeneratedFilterAsync(CreateSubscriptionOptions options, RuleFilter filter, CancellationToken cancellationToken)
    {
        IList<RuleProperties> rules = await GetRulesAsync(options.TopicName, options.SubscriptionName, cancellationToken).ConfigureAwait(false);
        if (rules.Count != 1 || !Guid.TryParse(rules[0].Name, out _))
            throw new InvalidOperationException(
                $"Cannot reconcile the generated filter for subscription '{options.SubscriptionName}' on topic '{options.TopicName}': expected one generated rule, found {rules.Count} rule(s).");

        var existingRule = rules[0];
        if (!(existingRule.Filter?.Equals(filter) ?? filter == null))
        {
            LogContext.Debug?.Log("Updating subscription filter: {Rule} ({DescriptionFilter} -> {Filter})", existingRule.Name,
                existingRule.Filter?.ToString(), filter?.ToString());

            existingRule.Filter = filter;

            await UpdateRuleAsync(options.TopicName, options.SubscriptionName, existingRule, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Attempts to delete a subscription, treating absence as success and logging other failures.</summary>
    /// <param name="subscriptionOptions">Options identifying the topic and subscription.</param>
    /// <param name="cancellationToken">The token that cancels the deletion request.</param>
    /// <returns>A task that completes after the deletion attempt.</returns>
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

    /// <summary>Releases the namespace context and disposes its messaging client when the context owns it.</summary>
    /// <returns>A task that completes after any owned messaging-client cleanup.</returns>
    /// <remarks>Caller-owned clients supplied through host configuration remain open and must be disposed by their owner.</remarks>
    public async ValueTask DisposeAsync()
    {
        var address = _client.FullyQualifiedNamespace;

        try
        {
            TransportLogMessages.DisconnectHost(address);
        }
        catch (Exception)
        {
            // Diagnostic failures must not prevent owned cleanup.
        }

        if (_ownsClient)
            await _client.DisposeAsync().ConfigureAwait(false);

        try
        {
            TransportLogMessages.DisconnectedHost(address);
        }
        catch (Exception)
        {
            // Diagnostic failures must not turn successful cleanup into a failure.
        }
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
