using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.SqlTransport.Topology;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>
/// Defines the contract for client context.
/// </summary>
public interface ClientContext :
    PipeContext
{
    /// <summary>
    /// Gets the connection context value.
    /// </summary>
    ConnectionContext ConnectionContext { get; }

    /// <summary>
    /// Create a queue
    /// </summary>
    /// <param name="queue"></param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task<long> CreateQueueAsync(Queue queue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a topic
    /// </summary>
    /// <param name="topic"></param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task<long> CreateTopicAsync(Topic topic, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a topic subscription
    /// </summary>
    /// <param name="subscription"></param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task<long> CreateTopicSubscriptionAsync(TopicToTopicSubscription subscription, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a topic subscription to a queue
    /// </summary>
    /// <param name="subscription"></param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task<long> CreateQueueSubscriptionAsync(TopicToQueueSubscription subscription, CancellationToken cancellationToken = default);

    /// <summary>
    /// Purge the specified queue (including all queue types), returning the number of messages removed
    /// </summary>
    /// <param name="queueName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<long> PurgeQueueAsync(string queueName, CancellationToken cancellationToken);

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task SendAsync<T>(string queueName, SqlMessageSendContext<T> context, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Publishes a message to its configured consumers.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="topicName">The topic name value.</param>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task PublishAsync<T>(string topicName, SqlMessageSendContext<T> context, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Performs the receive messages operation.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="mode">The mode value.</param>
    /// <param name="messageLimit">The message limit value.</param>
    /// <param name="concurrentCount">The concurrent count value.</param>
    /// <param name="lockDuration">The lock duration value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<IEnumerable<SqlTransportMessage>> ReceiveMessagesAsync(string queueName, SqlReceiveMode mode, int messageLimit, int concurrentCount,
        TimeSpan lockDuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the touch queue operation.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task TouchQueueAsync(string queueName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Move any messages that have either expired or exceeded their delivery count to the dead-letter queue
    /// </summary>
    /// <param name="queueName"></param>
    /// <param name="messageCount"></param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task<int?> DeadLetterQueueAsync(string queueName, int messageCount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the delete message operation.
    /// </summary>
    /// <param name="lockId">The lock id value.</param>
    /// <param name="messageDeliveryId">The message delivery id value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<bool> DeleteMessageAsync(Guid lockId, long messageDeliveryId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs the delete scheduled message operation.
    /// </summary>
    /// <param name="tokenId">The token id value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<bool> DeleteScheduledMessageAsync(Guid tokenId, CancellationToken cancellationToken);
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
    Task<bool> MoveMessageAsync(Guid lockId, long messageDeliveryId, string queueName, SqlQueueType queueType, DateTimeOffset? expirationTime,
        SendHeaders sendHeaders, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs the renew lock operation.
    /// </summary>
    /// <param name="lockId">The lock id value.</param>
    /// <param name="messageDeliveryId">The message delivery id value.</param>
    /// <param name="duration">The duration value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<bool> RenewLockAsync(Guid lockId, long messageDeliveryId, TimeSpan duration, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs the unlock operation.
    /// </summary>
    /// <param name="lockId">The lock id value.</param>
    /// <param name="messageDeliveryId">The message delivery id value.</param>
    /// <param name="delay">The delay value.</param>
    /// <param name="sendHeaders">The send headers value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<bool> UnlockAsync(Guid lockId, long messageDeliveryId, TimeSpan delay, SendHeaders sendHeaders, CancellationToken cancellationToken = default);
}
