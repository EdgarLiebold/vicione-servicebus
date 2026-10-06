using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.SqlTransport.Topology;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>Exposes state for client operations.</summary>
public interface ClientContext :
    PipeContext
{
    /// <summary>Gets the connection context.</summary>
    ConnectionContext ConnectionContext { get; }

    /// <summary>Create a queue.</summary>
    /// <param name="queue">The queue.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the created value.</returns>
    Task<long> CreateQueueAsync(Queue queue, CancellationToken cancellationToken = default);

    /// <summary>Create a topic.</summary>
    /// <param name="topic">The topic.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the created value.</returns>
    Task<long> CreateTopicAsync(Topic topic, CancellationToken cancellationToken = default);

    /// <summary>Create a topic subscription.</summary>
    /// <param name="subscription">The subscription.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the created value.</returns>
    Task<long> CreateTopicSubscriptionAsync(TopicToTopicSubscription subscription, CancellationToken cancellationToken = default);

    /// <summary>Create a topic subscription to a queue.</summary>
    /// <param name="subscription">The subscription.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the created value.</returns>
    Task<long> CreateQueueSubscriptionAsync(TopicToQueueSubscription subscription, CancellationToken cancellationToken = default);

    /// <summary>Purges pending deliveries from the specified primary queue, preserving its error and dead-letter queues, and returns the number removed.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the purge queue outcome.</returns>
    Task<long> PurgeQueueAsync(string queueName, CancellationToken cancellationToken);

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="queueName">The queue name.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SendAsync<T>(string queueName, SqlMessageSendContext<T> context, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="topicName">The topic name.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PublishAsync<T>(string topicName, SqlMessageSendContext<T> context, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Receives messages.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="mode">The mode.</param>
    /// <param name="messageLimit">The message limit.</param>
    /// <param name="concurrentCount">The concurrent count.</param>
    /// <param name="lockDuration">The lock duration.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the receive messages outcome.</returns>
    Task<IEnumerable<SqlTransportMessage>> ReceiveMessagesAsync(string queueName, SqlReceiveMode mode, int messageLimit, int concurrentCount,
        TimeSpan lockDuration, CancellationToken cancellationToken = default);

    /// <summary>Records usage of the specified primary queue through the provider.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task TouchQueueAsync(string queueName, CancellationToken cancellationToken = default);

    /// <summary>Move any messages that have either expired or exceeded their delivery count to the dead-letter queue.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="messageCount">The message count.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the dead letter queue outcome.</returns>
    Task<int?> DeadLetterQueueAsync(string queueName, int messageCount, CancellationToken cancellationToken = default);

    /// <summary>Deletes message.</summary>
    /// <param name="lockId">The lock id.</param>
    /// <param name="messageDeliveryId">The message delivery id.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the delete message outcome.</returns>
    Task<bool> DeleteMessageAsync(Guid lockId, long messageDeliveryId, CancellationToken cancellationToken = default);
    /// <summary>Deletes scheduled message.</summary>
    /// <param name="tokenId">The token id.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the delete scheduled message outcome.</returns>
    Task<bool> DeleteScheduledMessageAsync(Guid tokenId, CancellationToken cancellationToken);
    /// <summary>Moves message.</summary>
    /// <param name="lockId">The lock id.</param>
    /// <param name="messageDeliveryId">The message delivery id.</param>
    /// <param name="queueName">The queue name.</param>
    /// <param name="queueType">The runtime queue type used by the operation.</param>
    /// <param name="expirationTime">The expiration time.</param>
    /// <param name="sendHeaders">The send headers.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the move message outcome.</returns>
    Task<bool> MoveMessageAsync(Guid lockId, long messageDeliveryId, string queueName, SqlQueueType queueType, DateTimeOffset? expirationTime,
        SendHeaders sendHeaders, CancellationToken cancellationToken = default);
    /// <summary>Renews lock.</summary>
    /// <param name="lockId">The lock id.</param>
    /// <param name="messageDeliveryId">The message delivery id.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the renew lock outcome.</returns>
    Task<bool> RenewLockAsync(Guid lockId, long messageDeliveryId, TimeSpan duration, CancellationToken cancellationToken = default);
    /// <summary>Releases the current lock.</summary>
    /// <param name="lockId">The lock id.</param>
    /// <param name="messageDeliveryId">The message delivery id.</param>
    /// <param name="delay">The delay before the operation is attempted.</param>
    /// <param name="sendHeaders">The send headers.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the unlock outcome.</returns>
    Task<bool> UnlockAsync(Guid lockId, long messageDeliveryId, TimeSpan delay, SendHeaders sendHeaders, CancellationToken cancellationToken = default);
}
