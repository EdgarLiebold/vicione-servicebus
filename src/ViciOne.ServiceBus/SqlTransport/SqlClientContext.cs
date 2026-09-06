using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.SqlTransport.Topology;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>Carries state for sql client operations.</summary>
public abstract class SqlClientContext :
    ScopePipeContext,
    ClientContext
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    protected SqlClientContext(ConnectionContext context, CancellationToken cancellationToken)
        : base(context)
    {
        ConnectionContext = context;
        CancellationToken = cancellationToken;
    }

    /// <summary>Gets the cancellation token.</summary>
    public override CancellationToken CancellationToken { get; }

    /// <summary>Gets the connection context.</summary>
    public ConnectionContext ConnectionContext { get; }

    /// <summary>Creates queue.</summary>
    /// <param name="queue">The queue.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the created value.</returns>
    public abstract Task<long> CreateQueueAsync(Queue queue, CancellationToken cancellationToken = default);
    /// <summary>Creates topic.</summary>
    /// <param name="topic">The topic.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the created value.</returns>
    public abstract Task<long> CreateTopicAsync(Topic topic, CancellationToken cancellationToken = default);
    /// <summary>Creates topic subscription.</summary>
    /// <param name="subscription">The subscription.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the created value.</returns>
    public abstract Task<long> CreateTopicSubscriptionAsync(TopicToTopicSubscription subscription, CancellationToken cancellationToken = default);
    /// <summary>Creates queue subscription.</summary>
    /// <param name="subscription">The subscription.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the created value.</returns>
    public abstract Task<long> CreateQueueSubscriptionAsync(TopicToQueueSubscription subscription, CancellationToken cancellationToken = default);
    /// <summary>Purges queue.</summary>
    /// <param name="queueName">The queue name.</param>
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

    /// <summary>Receives messages.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="mode">The mode.</param>
    /// <param name="messageLimit">The message limit.</param>
    /// <param name="concurrentLimit">The concurrent limit.</param>
    /// <param name="lockDuration">The lock duration.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the receive messages outcome.</returns>
    public abstract Task<IEnumerable<SqlTransportMessage>> ReceiveMessagesAsync(string queueName, SqlReceiveMode mode, int messageLimit, int concurrentLimit,
        TimeSpan lockDuration, CancellationToken cancellationToken = default);

    /// <summary>Converts this value to uch queue.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public abstract Task TouchQueueAsync(string queueName, CancellationToken cancellationToken = default);
    /// <summary>Moves to the dead-letter destination queue.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="messageCount">The message count.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the dead letter queue outcome.</returns>
    public abstract Task<int?> DeadLetterQueueAsync(string queueName, int messageCount, CancellationToken cancellationToken = default);

    /// <summary>Deletes message.</summary>
    /// <param name="lockId">The lock id.</param>
    /// <param name="messageDeliveryId">The message delivery id.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the delete message outcome.</returns>
    public abstract Task<bool> DeleteMessageAsync(Guid lockId, long messageDeliveryId, CancellationToken cancellationToken = default);
    /// <summary>Deletes scheduled message.</summary>
    /// <param name="tokenId">The token id.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the delete scheduled message outcome.</returns>
    public abstract Task<bool> DeleteScheduledMessageAsync(Guid tokenId, CancellationToken cancellationToken);
    /// <summary>Moves message.</summary>
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
    /// <summary>Renews lock.</summary>
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
}
