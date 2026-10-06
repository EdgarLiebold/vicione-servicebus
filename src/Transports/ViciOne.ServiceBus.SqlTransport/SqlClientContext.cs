using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.SqlTransport.Topology;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>Defines provider operations executed within one supervised SQL client lifetime.</summary>
public abstract class SqlClientContext :
    ScopePipeContext,
    ClientContext
{
    /// <summary>Initializes a client for a supervised connection context.</summary>
    /// <param name="context">The provider connection context.</param>
    /// <param name="cancellationToken">The token that ends the client lifetime.</param>
    protected SqlClientContext(ConnectionContext context, CancellationToken cancellationToken)
        : base(context)
    {
        ArgumentNullException.ThrowIfNull(context);

        ConnectionContext = context;
        CancellationToken = cancellationToken;
    }

    /// <summary>Gets the token that ends this client lifetime.</summary>
    public override CancellationToken CancellationToken { get; }

    /// <summary>Gets the provider connection context used by this client.</summary>
    public ConnectionContext ConnectionContext { get; }

    /// <summary>Creates or resolves a queue.</summary>
    /// <param name="queue">The queue topology definition.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the created value.</returns>
    public abstract Task<long> CreateQueueAsync(Queue queue, CancellationToken cancellationToken = default);
    /// <summary>Creates or resolves a topic.</summary>
    /// <param name="topic">The topic topology definition.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the created value.</returns>
    public abstract Task<long> CreateTopicAsync(Topic topic, CancellationToken cancellationToken = default);
    /// <summary>Creates or resolves a topic-to-topic subscription.</summary>
    /// <param name="subscription">The subscription topology definition.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the created value.</returns>
    public abstract Task<long> CreateTopicSubscriptionAsync(TopicToTopicSubscription subscription, CancellationToken cancellationToken = default);
    /// <summary>Creates or resolves a topic-to-queue subscription.</summary>
    /// <param name="subscription">The subscription topology definition.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the created value.</returns>
    public abstract Task<long> CreateQueueSubscriptionAsync(TopicToQueueSubscription subscription, CancellationToken cancellationToken = default);
    /// <summary>Removes pending deliveries from a primary queue.</summary>
    /// <param name="queueName">The primary queue name.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the purge queue outcome.</returns>
    public abstract Task<long> PurgeQueueAsync(string queueName, CancellationToken cancellationToken);

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="queueName">The queue name.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public abstract Task SendAsync<T>(string queueName, SqlMessageSendContext<T> context, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="topicName">The topic name.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public abstract Task PublishAsync<T>(string topicName, SqlMessageSendContext<T> context, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Acquires a batch of eligible deliveries.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="mode">The mode.</param>
    /// <param name="messageLimit">The message limit.</param>
    /// <param name="concurrentLimit">The concurrent limit.</param>
    /// <param name="lockDuration">The lock duration.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the receive messages outcome.</returns>
    public abstract Task<IEnumerable<SqlTransportMessage>> ReceiveMessagesAsync(string queueName, SqlReceiveMode mode, int messageLimit, int concurrentLimit,
        TimeSpan lockDuration, CancellationToken cancellationToken = default);

    /// <summary>Records usage of the specified primary queue through the provider.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public abstract Task TouchQueueAsync(string queueName, CancellationToken cancellationToken = default);
    /// <summary>Moves exhausted deliveries to the dead-letter queue.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="messageCount">The message count.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the dead letter queue outcome.</returns>
    public abstract Task<int?> DeadLetterQueueAsync(string queueName, int messageCount, CancellationToken cancellationToken = default);

    /// <summary>Completes a locked delivery.</summary>
    /// <param name="lockId">The lock id.</param>
    /// <param name="messageDeliveryId">The message delivery id.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the delete message outcome.</returns>
    public abstract Task<bool> DeleteMessageAsync(Guid lockId, long messageDeliveryId, CancellationToken cancellationToken = default);
    /// <summary>Cancels an untouched scheduled message.</summary>
    /// <param name="tokenId">The token id.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the delete scheduled message outcome.</returns>
    public abstract Task<bool> DeleteScheduledMessageAsync(Guid tokenId, CancellationToken cancellationToken);
    /// <summary>Moves a locked delivery to another queue category.</summary>
    /// <param name="lockId">The lock id.</param>
    /// <param name="messageDeliveryId">The message delivery id.</param>
    /// <param name="queueName">The queue name.</param>
    /// <param name="queueType">The runtime queue type used by the operation.</param>
    /// <param name="expirationTime">The expiration time.</param>
    /// <param name="sendHeaders">The send headers.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the move message outcome.</returns>
    public abstract Task<bool> MoveMessageAsync(Guid lockId, long messageDeliveryId, string queueName, SqlQueueType queueType, DateTimeOffset? expirationTime,
        SendHeaders sendHeaders, CancellationToken cancellationToken = default);
    /// <summary>Renews a delivery lock.</summary>
    /// <param name="lockId">The lock id.</param>
    /// <param name="messageDeliveryId">The message delivery id.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the renew lock outcome.</returns>
    public abstract Task<bool> RenewLockAsync(Guid lockId, long messageDeliveryId, TimeSpan duration, CancellationToken cancellationToken = default);
    /// <summary>Releases the current lock.</summary>
    /// <param name="lockId">The lock id.</param>
    /// <param name="messageDeliveryId">The message delivery id.</param>
    /// <param name="delay">The delay before the operation is attempted.</param>
    /// <param name="sendHeaders">The send headers.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the unlock outcome.</returns>
    public abstract Task<bool> UnlockAsync(Guid lockId, long messageDeliveryId, TimeSpan delay, SendHeaders sendHeaders, CancellationToken cancellationToken = default);

    /// <summary>Executes a provider command governed by both the client lifetime and the caller's cancellation token.</summary>
    /// <typeparam name="T">The command result type.</typeparam>
    /// <param name="operation">The database operation to execute inside the provider transaction.</param>
    /// <param name="cancellationToken">The caller's cancellation token.</param>
    /// <returns>The database operation result.</returns>
    protected async Task<T> ExecuteDatabaseOperationAsync<T>(
        Func<IDbConnection, IDbTransaction, CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        if (!CancellationToken.CanBeCanceled)
        {
            return await ConnectionContext.QueryAsync(
                (connection, transaction) => operation(connection, transaction, cancellationToken), cancellationToken).ConfigureAwait(false);
        }

        if (!cancellationToken.CanBeCanceled || cancellationToken == CancellationToken)
        {
            return await ConnectionContext.QueryAsync(
                (connection, transaction) => operation(connection, transaction, CancellationToken), CancellationToken).ConfigureAwait(false);
        }

        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await ConnectionContext.QueryAsync(
            (connection, transaction) => operation(connection, transaction, linkedSource.Token), linkedSource.Token).ConfigureAwait(false);
    }
}
