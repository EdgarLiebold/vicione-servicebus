using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Leases a namespace connection and links each administration request to the lease lifetime.</summary>
public class SharedConnectionContext :
    ProxyPipeContext,
    ConnectionContext
{
    readonly ConnectionContext _context;

    /// <summary>Initializes a lease over an existing namespace connection.</summary>
    /// <param name="context">The shared namespace connection.</param>
    /// <param name="cancellationToken">The token that bounds this lease.</param>
    public SharedConnectionContext(ConnectionContext context, CancellationToken cancellationToken)
        : base(context)
    {
        _context = context;

        CancellationToken = cancellationToken;
    }

    /// <summary>Gets the token that bounds this lease.</summary>
    public override CancellationToken CancellationToken { get; }

    /// <summary>Gets the shared connection's namespace URI.</summary>
    public Uri Endpoint => _context.Endpoint;

    /// <summary>Creates a non-session queue processor through the shared connection.</summary>
    /// <param name="settings">The queue and processor settings.</param>
    /// <returns>The configured queue processor.</returns>
    public ServiceBusProcessor CreateQueueProcessor(ReceiveSettings settings)
    {
        return _context.CreateQueueProcessor(settings);
    }

    /// <summary>Creates a session-aware queue processor through the shared connection.</summary>
    /// <param name="settings">The queue and session-processor settings.</param>
    /// <returns>The configured queue session processor.</returns>
    public ServiceBusSessionProcessor CreateQueueSessionProcessor(ReceiveSettings settings)
    {
        return _context.CreateQueueSessionProcessor(settings);
    }

    /// <summary>Creates a non-session subscription processor through the shared connection.</summary>
    /// <param name="settings">The subscription and processor settings.</param>
    /// <returns>The configured subscription processor.</returns>
    public ServiceBusProcessor CreateSubscriptionProcessor(SubscriptionSettings settings)
    {
        return _context.CreateSubscriptionProcessor(settings);
    }

    /// <summary>Creates a session-aware subscription processor through the shared connection.</summary>
    /// <param name="settings">The subscription and session-processor settings.</param>
    /// <returns>The configured subscription session processor.</returns>
    public ServiceBusSessionProcessor CreateSubscriptionSessionProcessor(SubscriptionSettings settings)
    {
        return _context.CreateSubscriptionSessionProcessor(settings);
    }

    /// <summary>Creates a sender through the shared connection.</summary>
    /// <param name="entityPath">The destination entity path.</param>
    /// <returns>The Azure Service Bus sender.</returns>
    public ServiceBusSender CreateMessageSender(string entityPath)
    {
        return _context.CreateMessageSender(entityPath);
    }

    /// <summary>Creates or retrieves a queue using cancellation linked to this connection lease.</summary>
    /// <param name="createQueueOptions">The desired queue properties.</param>
    /// <param name="cancellationToken">The caller's cancellation token.</param>
    /// <returns>A task that produces the queue properties.</returns>
    public async Task<QueueProperties> CreateQueueAsync(CreateQueueOptions createQueueOptions, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.CreateQueueAsync(createQueueOptions, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>Creates or retrieves a topic using cancellation linked to this connection lease.</summary>
    /// <param name="createTopicOptions">The desired topic properties.</param>
    /// <param name="cancellationToken">The caller's cancellation token.</param>
    /// <returns>A task that produces the topic properties.</returns>
    public async Task<TopicProperties> CreateTopicAsync(CreateTopicOptions createTopicOptions, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.CreateTopicAsync(createTopicOptions, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>Creates or reconciles a subscription using cancellation linked to this connection lease.</summary>
    /// <param name="createSubscriptionOptions">The desired subscription properties.</param>
    /// <param name="rule">An optional complete initial rule.</param>
    /// <param name="filter">An optional filter for the generated initial rule.</param>
    /// <param name="cancellationToken">The caller's cancellation token.</param>
    /// <returns>A task that produces the subscription properties.</returns>
    public async Task<SubscriptionProperties> CreateTopicSubscriptionAsync(CreateSubscriptionOptions createSubscriptionOptions, CreateRuleOptions? rule,
        RuleFilter? filter, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.CreateTopicSubscriptionAsync(createSubscriptionOptions, rule, filter, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>Deletes a subscription using cancellation linked to this connection lease.</summary>
    /// <param name="subscriptionOptions">Options identifying the topic and subscription.</param>
    /// <param name="cancellationToken">The caller's cancellation token.</param>
    /// <returns>A task that completes after the deletion attempt.</returns>
    public async Task DeleteTopicSubscriptionAsync(CreateSubscriptionOptions subscriptionOptions, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.DeleteTopicSubscriptionAsync(subscriptionOptions, tokenSource.Token).ConfigureAwait(false);
    }
}
