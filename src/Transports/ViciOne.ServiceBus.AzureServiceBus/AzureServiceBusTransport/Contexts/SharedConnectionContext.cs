using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Provides a shared connection context implementation.
/// </summary>
public class SharedConnectionContext :
    ProxyPipeContext,
    ConnectionContext
{
    readonly ConnectionContext _context;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public SharedConnectionContext(ConnectionContext context, CancellationToken cancellationToken)
        : base(context)
    {
        _context = context;

        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// Gets the cancellation token value.
    /// </summary>
    public override CancellationToken CancellationToken { get; }

    /// <summary>
    /// Gets the endpoint value.
    /// </summary>
    public Uri Endpoint => _context.Endpoint;

    /// <summary>
    /// Creates queue processor.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    /// <returns>The result of the operation.</returns>
    public ServiceBusProcessor CreateQueueProcessor(ReceiveSettings settings)
    {
        return _context.CreateQueueProcessor(settings);
    }

    /// <summary>
    /// Creates queue session processor.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    /// <returns>The result of the operation.</returns>
    public ServiceBusSessionProcessor CreateQueueSessionProcessor(ReceiveSettings settings)
    {
        return _context.CreateQueueSessionProcessor(settings);
    }

    /// <summary>
    /// Creates subscription processor.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    /// <returns>The result of the operation.</returns>
    public ServiceBusProcessor CreateSubscriptionProcessor(SubscriptionSettings settings)
    {
        return _context.CreateSubscriptionProcessor(settings);
    }

    /// <summary>
    /// Creates subscription session processor.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    /// <returns>The result of the operation.</returns>
    public ServiceBusSessionProcessor CreateSubscriptionSessionProcessor(SubscriptionSettings settings)
    {
        return _context.CreateSubscriptionSessionProcessor(settings);
    }

    /// <summary>
    /// Creates message sender.
    /// </summary>
    /// <param name="entityPath">The entity path value.</param>
    /// <returns>The result of the operation.</returns>
    public ServiceBusSender CreateMessageSender(string entityPath)
    {
        return _context.CreateMessageSender(entityPath);
    }

    /// <summary>
    /// Creates queue.
    /// </summary>
    /// <param name="createQueueOptions">The create queue options value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<QueueProperties> CreateQueueAsync(CreateQueueOptions createQueueOptions, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return _context.CreateQueueAsync(createQueueOptions, tokenSource.Token);
    }

    /// <summary>
    /// Creates topic.
    /// </summary>
    /// <param name="createTopicOptions">The create topic options value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<TopicProperties> CreateTopicAsync(CreateTopicOptions createTopicOptions, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return _context.CreateTopicAsync(createTopicOptions, tokenSource.Token);
    }

    /// <summary>
    /// Creates topic subscription.
    /// </summary>
    /// <param name="createSubscriptionOptions">The create subscription options value.</param>
    /// <param name="rule">The rule value.</param>
    /// <param name="filter">The filter value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<SubscriptionProperties> CreateTopicSubscriptionAsync(CreateSubscriptionOptions createSubscriptionOptions, CreateRuleOptions? rule,
        RuleFilter? filter, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return _context.CreateTopicSubscriptionAsync(createSubscriptionOptions, rule, filter, tokenSource.Token);
    }

    /// <summary>
    /// Performs the delete topic subscription operation.
    /// </summary>
    /// <param name="subscriptionOptions">The subscription options value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task DeleteTopicSubscriptionAsync(CreateSubscriptionOptions subscriptionOptions, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return _context.DeleteTopicSubscriptionAsync(subscriptionOptions, tokenSource.Token);
    }
}
