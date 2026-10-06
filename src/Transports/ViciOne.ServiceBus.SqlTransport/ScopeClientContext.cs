using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.SqlTransport.Topology;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>Leases a SQL client and links each database operation to the caller and lease cancellation tokens.</summary>
public class ScopeClientContext :
    ScopePipeContext,
    ClientContext
{
    readonly ClientContext _context;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public ScopeClientContext(ClientContext context, CancellationToken cancellationToken)
        : base(context)
    {
        _context = context;
        CancellationToken = cancellationToken;
    }

    /// <summary>Gets the cancellation token.</summary>
    public override CancellationToken CancellationToken { get; }

    /// <summary>Gets the connection context.</summary>
    public ConnectionContext ConnectionContext => _context.ConnectionContext;

    /// <summary>Creates queue.</summary>
    /// <param name="queue">The queue.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the created value.</returns>
    public async Task<long> CreateQueueAsync(Queue queue, CancellationToken cancellationToken = default)
    {
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);
        return await _context.CreateQueueAsync(queue, cancellationToken: linkedSource.Token).ConfigureAwait(false);
    }

    /// <summary>Creates topic.</summary>
    /// <param name="topic">The topic.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the created value.</returns>
    public async Task<long> CreateTopicAsync(Topic topic, CancellationToken cancellationToken = default)
    {
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);
        return await _context.CreateTopicAsync(topic, cancellationToken: linkedSource.Token).ConfigureAwait(false);
    }

    /// <summary>Creates topic subscription.</summary>
    /// <param name="subscription">The subscription.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the created value.</returns>
    public async Task<long> CreateTopicSubscriptionAsync(TopicToTopicSubscription subscription, CancellationToken cancellationToken = default)
    {
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);
        return await _context.CreateTopicSubscriptionAsync(subscription, cancellationToken: linkedSource.Token).ConfigureAwait(false);
    }

    /// <summary>Creates queue subscription.</summary>
    /// <param name="subscription">The subscription.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the created value.</returns>
    public async Task<long> CreateQueueSubscriptionAsync(TopicToQueueSubscription subscription, CancellationToken cancellationToken = default)
    {
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);
        return await _context.CreateQueueSubscriptionAsync(subscription, cancellationToken: linkedSource.Token).ConfigureAwait(false);
    }

    /// <summary>Purges queue.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the purge queue outcome.</returns>
    public async Task<long> PurgeQueueAsync(string queueName, CancellationToken cancellationToken)
    {
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);
        return await _context.PurgeQueueAsync(queueName, linkedSource.Token).ConfigureAwait(false);
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="queueName">The queue name.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendAsync<T>(string queueName, SqlMessageSendContext<T> context, CancellationToken cancellationToken = default)
        where T : class
    {
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);
        await _context.SendAsync(queueName, context, cancellationToken: linkedSource.Token).ConfigureAwait(false);
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="topicName">The topic name.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task PublishAsync<T>(string topicName, SqlMessageSendContext<T> context, CancellationToken cancellationToken = default)
        where T : class
    {
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);
        await _context.PublishAsync(topicName, context, cancellationToken: linkedSource.Token).ConfigureAwait(false);
    }

    /// <summary>Renews lock.</summary>
    /// <param name="lockId">The lock id.</param>
    /// <param name="messageDeliveryId">The message delivery id.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the renew lock outcome.</returns>
    public async Task<bool> RenewLockAsync(Guid lockId, long messageDeliveryId, TimeSpan duration, CancellationToken cancellationToken = default)
    {
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);
        return await _context.RenewLockAsync(lockId, messageDeliveryId, duration, cancellationToken: linkedSource.Token).ConfigureAwait(false);
    }

    /// <summary>Releases the current lock.</summary>
    /// <param name="lockId">The lock id.</param>
    /// <param name="messageDeliveryId">The message delivery id.</param>
    /// <param name="delay">The delay before the operation is attempted.</param>
    /// <param name="sendHeaders">The send headers.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the unlock outcome.</returns>
    public async Task<bool> UnlockAsync(Guid lockId, long messageDeliveryId, TimeSpan delay, SendHeaders sendHeaders, CancellationToken cancellationToken = default)
    {
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);
        return await _context.UnlockAsync(lockId, messageDeliveryId, delay, sendHeaders, cancellationToken: linkedSource.Token).ConfigureAwait(false);
    }

    /// <summary>Receives messages.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="mode">The mode.</param>
    /// <param name="messageLimit">The message limit.</param>
    /// <param name="concurrentLimit">The concurrent limit.</param>
    /// <param name="lockDuration">The lock duration.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the receive messages outcome.</returns>
    public async Task<IEnumerable<SqlTransportMessage>> ReceiveMessagesAsync(string queueName, SqlReceiveMode mode, int messageLimit, int concurrentLimit,
        TimeSpan lockDuration, CancellationToken cancellationToken = default)
    {
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);
        return await _context.ReceiveMessagesAsync(queueName, mode, messageLimit, concurrentLimit, lockDuration, cancellationToken: linkedSource.Token).ConfigureAwait(false);
    }

    /// <summary>Records usage of the specified primary queue through the provider.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task TouchQueueAsync(string queueName, CancellationToken cancellationToken = default)
    {
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);
        await _context.TouchQueueAsync(queueName, cancellationToken: linkedSource.Token).ConfigureAwait(false);
    }

    /// <summary>Moves to the dead-letter destination queue.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="messageCount">The message count.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the dead letter queue outcome.</returns>
    public async Task<int?> DeadLetterQueueAsync(string queueName, int messageCount, CancellationToken cancellationToken = default)
    {
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);
        return await _context.DeadLetterQueueAsync(queueName, messageCount, cancellationToken: linkedSource.Token).ConfigureAwait(false);
    }

    /// <summary>Deletes message.</summary>
    /// <param name="lockId">The lock id.</param>
    /// <param name="messageDeliveryId">The message delivery id.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the delete message outcome.</returns>
    public async Task<bool> DeleteMessageAsync(Guid lockId, long messageDeliveryId, CancellationToken cancellationToken = default)
    {
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);
        return await _context.DeleteMessageAsync(lockId, messageDeliveryId, cancellationToken: linkedSource.Token).ConfigureAwait(false);
    }

    /// <summary>Deletes scheduled message.</summary>
    /// <param name="tokenId">The token id.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the delete scheduled message outcome.</returns>
    public async Task<bool> DeleteScheduledMessageAsync(Guid tokenId, CancellationToken cancellationToken)
    {
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);
        return await _context.DeleteScheduledMessageAsync(tokenId, linkedSource.Token).ConfigureAwait(false);
    }

    /// <summary>Moves message.</summary>
    /// <param name="lockId">The lock id.</param>
    /// <param name="messageDeliveryId">The message delivery id.</param>
    /// <param name="queueName">The queue name.</param>
    /// <param name="queueType">The runtime queue type used by the operation.</param>
    /// <param name="expirationTime">The expiration time.</param>
    /// <param name="sendHeaders">The send headers.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the move message outcome.</returns>
    public async Task<bool> MoveMessageAsync(Guid lockId, long messageDeliveryId, string queueName, SqlQueueType queueType, DateTimeOffset? expirationTime,
        SendHeaders sendHeaders, CancellationToken cancellationToken = default)
    {
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);
        return await _context.MoveMessageAsync(lockId, messageDeliveryId, queueName, queueType, expirationTime, sendHeaders, cancellationToken: linkedSource.Token).ConfigureAwait(false);
    }
}
