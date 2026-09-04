using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.SqlTransport.Topology;

namespace ViciOne.ServiceBus.SqlTransport;

public interface ClientContext :
    PipeContext
{
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

    Task SendAsync<T>(string queueName, SqlMessageSendContext<T> context, CancellationToken cancellationToken = default)
        where T : class;

    Task PublishAsync<T>(string topicName, SqlMessageSendContext<T> context, CancellationToken cancellationToken = default)
        where T : class;

    Task<IEnumerable<SqlTransportMessage>> ReceiveMessagesAsync(string queueName, SqlReceiveMode mode, int messageLimit, int concurrentCount,
        TimeSpan lockDuration, CancellationToken cancellationToken = default);

    Task TouchQueueAsync(string queueName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Move any messages that have either expired or exceeded their delivery count to the dead-letter queue
    /// </summary>
    /// <param name="queueName"></param>
    /// <param name="messageCount"></param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task<int?> DeadLetterQueueAsync(string queueName, int messageCount, CancellationToken cancellationToken = default);

    Task<bool> DeleteMessageAsync(Guid lockId, long messageDeliveryId, CancellationToken cancellationToken = default);
    Task<bool> DeleteScheduledMessageAsync(Guid tokenId, CancellationToken cancellationToken);
    Task<bool> MoveMessageAsync(Guid lockId, long messageDeliveryId, string queueName, SqlQueueType queueType, DateTimeOffset? expirationTime,
        SendHeaders sendHeaders, CancellationToken cancellationToken = default);
    Task<bool> RenewLockAsync(Guid lockId, long messageDeliveryId, TimeSpan duration, CancellationToken cancellationToken = default);
    Task<bool> UnlockAsync(Guid lockId, long messageDeliveryId, TimeSpan delay, SendHeaders sendHeaders, CancellationToken cancellationToken = default);
}
