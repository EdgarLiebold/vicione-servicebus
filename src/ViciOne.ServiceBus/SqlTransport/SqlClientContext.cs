using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.SqlTransport.Topology;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>
/// Provides a sql client context implementation.
/// </summary>
public abstract class SqlClientContext :
    ScopePipeContext,
    ClientContext
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    protected SqlClientContext(ConnectionContext context, CancellationToken cancellationToken)
        : base(context)
    {
        ConnectionContext = context;
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// Gets the cancellation token value.
    /// </summary>
    public override CancellationToken CancellationToken { get; }

    /// <summary>
    /// Gets the connection context value.
    /// </summary>
    public ConnectionContext ConnectionContext { get; }

    /// <summary>
    /// Creates queue.
    /// </summary>
    /// <param name="queue">The queue value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public abstract Task<long> CreateQueueAsync(Queue queue, CancellationToken cancellationToken = default);
    /// <summary>
    /// Creates topic.
    /// </summary>
    /// <param name="topic">The topic value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public abstract Task<long> CreateTopicAsync(Topic topic, CancellationToken cancellationToken = default);
    /// <summary>
    /// Creates topic subscription.
    /// </summary>
    /// <param name="subscription">The subscription value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public abstract Task<long> CreateTopicSubscriptionAsync(TopicToTopicSubscription subscription, CancellationToken cancellationToken = default);
    /// <summary>
    /// Creates queue subscription.
    /// </summary>
    /// <param name="subscription">The subscription value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public abstract Task<long> CreateQueueSubscriptionAsync(TopicToQueueSubscription subscription, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs the purge queue operation.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public abstract Task<long> PurgeQueueAsync(string queueName, CancellationToken cancellationToken);

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public abstract Task SendAsync<T>(string queueName, SqlMessageSendContext<T> context, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Publishes a message to its configured consumers.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="topicName">The topic name value.</param>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public abstract Task PublishAsync<T>(string topicName, SqlMessageSendContext<T> context, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Performs the receive messages operation.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="mode">The mode value.</param>
    /// <param name="messageLimit">The message limit value.</param>
    /// <param name="concurrentLimit">The concurrent limit value.</param>
    /// <param name="lockDuration">The lock duration value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public abstract Task<IEnumerable<SqlTransportMessage>> ReceiveMessagesAsync(string queueName, SqlReceiveMode mode, int messageLimit, int concurrentLimit,
        TimeSpan lockDuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the touch queue operation.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public abstract Task TouchQueueAsync(string queueName, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs the dead letter queue operation.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="messageCount">The message count value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public abstract Task<int?> DeadLetterQueueAsync(string queueName, int messageCount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the delete message operation.
    /// </summary>
    /// <param name="lockId">The lock id value.</param>
    /// <param name="messageDeliveryId">The message delivery id value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public abstract Task<bool> DeleteMessageAsync(Guid lockId, long messageDeliveryId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs the delete scheduled message operation.
    /// </summary>
    /// <param name="tokenId">The token id value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public abstract Task<bool> DeleteScheduledMessageAsync(Guid tokenId, CancellationToken cancellationToken);
    /// <summary>
    /// Performs the move message operation.
    /// </summary>
    /// <param name="lockId">The lock id value.</param>
    /// <param name="messageDeliveryId">The message delivery id value.</param>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="queueType">The queue type value.</param>
    /// <param name="expirationTime">The expiration time value.</param>
    /// <param name="sendHeaders">The send headers value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public abstract Task<bool> MoveMessageAsync(Guid lockId, long messageDeliveryId, string queueName, SqlQueueType queueType, DateTimeOffset? expirationTime,
        SendHeaders sendHeaders, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs the renew lock operation.
    /// </summary>
    /// <param name="lockId">The lock id value.</param>
    /// <param name="messageDeliveryId">The message delivery id value.</param>
    /// <param name="duration">The duration value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public abstract Task<bool> RenewLockAsync(Guid lockId, long messageDeliveryId, TimeSpan duration, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs the unlock operation.
    /// </summary>
    /// <param name="lockId">The lock id value.</param>
    /// <param name="messageDeliveryId">The message delivery id value.</param>
    /// <param name="delay">The delay value.</param>
    /// <param name="sendHeaders">The send headers value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public abstract Task<bool> UnlockAsync(Guid lockId, long messageDeliveryId, TimeSpan delay, SendHeaders sendHeaders, CancellationToken cancellationToken = default);
}
