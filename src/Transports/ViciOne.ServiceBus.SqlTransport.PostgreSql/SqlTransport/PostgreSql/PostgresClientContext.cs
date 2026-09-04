using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.SqlTransport.Topology;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql;

/// <summary>
/// Provides a postgres client context implementation.
/// </summary>
public class PostgresClientContext :
    SqlClientContext
{
    readonly Guid _consumerId;
    readonly PostgresDbConnectionContext _context;
    readonly string _createQueueSql;
    readonly string _createQueueSubscriptionSql;
    readonly string _createTopicSql;
    readonly string _createTopicSubscriptionSql;
    readonly string _deleteMessageSql;
    readonly string _deleteScheduledMessageSql;
    readonly string _moveMessageTypeSql;
    readonly string _publishSql;
    readonly string _purgeQueueSql;
    readonly string _receivePartitionedSql;
    readonly string _receiveSql;
    readonly string _renewLockSql;
    readonly string _sendSql;
    readonly string _touchQueueSql;
    readonly string _unlockSql;
    readonly string _deadLetterMessagesSql;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public PostgresClientContext(PostgresDbConnectionContext context, CancellationToken cancellationToken)
        : base(context, cancellationToken)
    {
        _context = context;
        _consumerId = NewId.NextGuid();

        _createQueueSubscriptionSql = string.Format(SqlStatements.DbCreateQueueSubscriptionSql, _context.Schema);
        _receiveSql = string.Format(SqlStatements.DbReceiveSql, _context.Schema);
        _receivePartitionedSql = string.Format(SqlStatements.DbReceivePartitionedSql, _context.Schema);
        _sendSql = string.Format(SqlStatements.DbEnqueueSql, _context.Schema);
        _createTopicSubscriptionSql = string.Format(SqlStatements.DbCreateTopicSubscriptionSql, _context.Schema);
        _publishSql = string.Format(SqlStatements.DbPublishSql, _context.Schema);
        _purgeQueueSql = string.Format(SqlStatements.DbPurgeQueueSql, _context.Schema);
        _deadLetterMessagesSql = string.Format(SqlStatements.DbDeadLetterMessagesSql, _context.Schema);
        _createTopicSql = string.Format(SqlStatements.DbCreateTopicSql, _context.Schema);
        _createQueueSql = string.Format(SqlStatements.DbCreateQueueSql, _context.Schema);
        _deleteMessageSql = string.Format(SqlStatements.DbDeleteMessageSql, _context.Schema);
        _deleteScheduledMessageSql = string.Format(SqlStatements.DbDeleteScheduledMessageSql, _context.Schema);
        _moveMessageTypeSql = string.Format(SqlStatements.DbMoveMessageSql, _context.Schema);
        _renewLockSql = string.Format(SqlStatements.DbRenewLockSql, _context.Schema);
        _touchQueueSql = string.Format(SqlStatements.DbTouchQueueSql, _context.Schema);
        _unlockSql = string.Format(SqlStatements.DbUnlockSql, _context.Schema);
    }

    /// <summary>
    /// Creates queue.
    /// </summary>
    /// <param name="queue">The queue value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public override Task<long> CreateQueueAsync(Queue queue, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<long>(cancellationToken); return _context.QueryAsync((x, t) => x.ExecuteScalarAsync<long>(_createQueueSql, new
        {
            queue_name = queue.QueueName,
            auto_delete = (int?)queue.AutoDeleteOnIdle?.TotalSeconds,
            max_delivery_count = queue.MaxDeliveryCount
        }, t), CancellationToken);
    }

    /// <summary>
    /// Creates topic.
    /// </summary>
    /// <param name="topic">The topic value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public override Task<long> CreateTopicAsync(Topic topic, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<long>(cancellationToken); return _context.QueryAsync((x, t) => x.ExecuteScalarAsync<long>(_createTopicSql, new { topic_name = topic.TopicName }), CancellationToken);
    }

    /// <summary>
    /// Creates topic subscription.
    /// </summary>
    /// <param name="subscription">The subscription value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public override Task<long> CreateTopicSubscriptionAsync(TopicToTopicSubscription subscription, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<long>(cancellationToken); return _context.QueryAsync((x, t) => x.ExecuteScalarAsync<long>(_createTopicSubscriptionSql, new
        {
            source_topic_name = subscription.Source.TopicName,
            destination_topic_name = subscription.Destination.TopicName,
            type = (int)subscription.SubscriptionType,
            routing_key = subscription.RoutingKey,
            filter = new JsonParameter(null)
        }), CancellationToken);
    }

    /// <summary>
    /// Creates queue subscription.
    /// </summary>
    /// <param name="subscription">The subscription value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public override Task<long> CreateQueueSubscriptionAsync(TopicToQueueSubscription subscription, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<long>(cancellationToken); return _context.QueryAsync((x, t) => x.ExecuteScalarAsync<long>(_createQueueSubscriptionSql, new
        {
            source_topic_name = subscription.Source.TopicName,
            destination_queue_name = subscription.Destination.QueueName,
            type = (int)subscription.SubscriptionType,
            routing_key = subscription.RoutingKey,
            filter = new JsonParameter(null)
        }), CancellationToken);
    }

    /// <summary>
    /// Performs the purge queue operation.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public override Task<long> PurgeQueueAsync(string queueName, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<long>(cancellationToken); return _context.QueryAsync((x, t) => x.ExecuteScalarAsync<long>(_purgeQueueSql, new { queue_name = queueName }), CancellationToken);
    }

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
    public override async Task<IEnumerable<SqlTransportMessage>> ReceiveMessagesAsync(string queueName, SqlReceiveMode mode, int messageLimit,
        int concurrentLimit, TimeSpan lockDuration, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); try
        {
            if (mode == SqlReceiveMode.Normal)
            {
                return await _context.QueryAsync((x, t) => x.QueryAsync<SqlTransportMessage>(_receiveSql, new
                {
                    queue_name = queueName,
                    fetch_consumer_id = _consumerId,
                    fetch_lock_id = NewId.NextGuid(),
                    lock_duration = lockDuration,
                    fetch_count = messageLimit
                }), CancellationToken).ConfigureAwait(false);
            }

            var ordered = mode switch
            {
                SqlReceiveMode.PartitionedOrdered => 1,
                SqlReceiveMode.PartitionedOrderedConcurrent => 1,
                _ => 0
            };

            return await _context.QueryAsync((x, t) => x.QueryAsync<SqlTransportMessage>(_receivePartitionedSql, new
            {
                queue_name = queueName,
                fetch_consumer_id = _consumerId,
                fetch_lock_id = NewId.NextGuid(),
                lock_duration = lockDuration,
                fetch_count = messageLimit,
                concurrent_count = concurrentLimit,
                ordered
            }), CancellationToken).ConfigureAwait(false);
        }
        catch (PostgresException exception) when (exception.ErrorCode == 40001)
        {
            return [];
        }
    }

    /// <summary>
    /// Performs the touch queue operation.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public override Task TouchQueueAsync(string queueName, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return _context.QueryAsync((x, t) => x.ExecuteScalarAsync<int?>(_touchQueueSql, new { queue_name = queueName }), CancellationToken);
    }

    /// <summary>
    /// Performs the dead letter queue operation.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="messageCount">The message count value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public override Task<int?> DeadLetterQueueAsync(string queueName, int messageCount, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<int?>(cancellationToken); return _context.QueryAsync((x, t) => x.ExecuteScalarAsync<int?>(_deadLetterMessagesSql, new
        {
            queue_name = queueName,
            message_count = messageCount
        }), CancellationToken);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public override Task SendAsync<T>(string queueName, SqlMessageSendContext<T> context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); IEnumerable<KeyValuePair<string, object>> headers = context.Headers.GetAll().ToList();
        var headersAsJson = headers.Any() ? JsonSerializer.Serialize(headers, ServiceBusMetadataJson.Options) : null;

        Guid? schedulingTokenId = context.Headers.Get<Guid>(MessageHeaders.SchedulingTokenId);
        DateTime? expirationTime = context.TimeToLive.HasValue
            ? context.GetTimeProvider().GetUtcNow().UtcDateTime + context.TimeToLive.Value
            : null;

        return _context.QueryAsync((x, t) => x.ExecuteScalarAsync<long?>(_sendSql, new
        {
            entity_name = queueName,
            priority = (int)(context.Priority ?? 100),
            transport_message_id = context.TransportMessageId,
            body = new JsonParameter(context.Body.GetString()),
            binary_body = default(byte[]?),
            content_type = context.ContentType?.MediaType,
            message_type = string.Join(";", context.SupportedMessageTypes),
            message_id = context.MessageId,
            correlation_id = context.CorrelationId,
            conversation_id = context.ConversationId,
            request_id = context.RequestId,
            initiator_id = context.InitiatorId,
            source_address = context.SourceAddress,
            destination_address = context.DestinationAddress,
            response_address = context.ResponseAddress,
            fault_address = context.FaultAddress,
            sent_time = context.SentTime,
            expiration_time = expirationTime,
            headers = new JsonParameter(headersAsJson),
            host = new JsonParameter(HostInfoCache.HostInfoJson),
            partition_key = context.PartitionKey,
            routing_key = context.RoutingKey,
            delay = context.Delay,
            scheduling_token_id = schedulingTokenId
        }), CancellationToken);
    }

    /// <summary>
    /// Publishes a message to its configured consumers.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="topicName">The topic name value.</param>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public override Task PublishAsync<T>(string topicName, SqlMessageSendContext<T> context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); IEnumerable<KeyValuePair<string, object>> headers = context.Headers.GetAll().ToList();
        var headersAsJson = headers.Any() ? JsonSerializer.Serialize(headers, ServiceBusMetadataJson.Options) : null;

        Guid? schedulingTokenId = context.Headers.Get<Guid>(MessageHeaders.SchedulingTokenId);
        DateTime? expirationTime = context.TimeToLive.HasValue
            ? context.GetTimeProvider().GetUtcNow().UtcDateTime + context.TimeToLive.Value
            : null;

        return _context.QueryAsync((x, t) => x.ExecuteScalarAsync<long?>(_publishSql, new
        {
            entity_name = topicName,
            priority = (int)(context.Priority ?? 100),
            transport_message_id = context.TransportMessageId,
            body = new JsonParameter(context.Body.GetString()),
            binary_body = default(byte[]?),
            content_type = context.ContentType?.MediaType,
            message_type = string.Join(";", context.SupportedMessageTypes),
            message_id = context.MessageId,
            correlation_id = context.CorrelationId,
            conversation_id = context.ConversationId,
            request_id = context.RequestId,
            initiator_id = context.InitiatorId,
            source_address = context.SourceAddress,
            destination_address = context.DestinationAddress,
            response_address = context.ResponseAddress,
            fault_address = context.FaultAddress,
            sent_time = context.SentTime,
            expiration_time = expirationTime,
            headers = new JsonParameter(headersAsJson),
            host = new JsonParameter(HostInfoCache.HostInfoJson),
            partition_key = context.PartitionKey,
            routing_key = context.RoutingKey,
            delay = context.Delay,
            scheduling_token_id = schedulingTokenId
        }), CancellationToken);
    }

    /// <summary>
    /// Performs the delete message operation.
    /// </summary>
    /// <param name="lockId">The lock id value.</param>
    /// <param name="messageDeliveryId">The message delivery id value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public override async Task<bool> DeleteMessageAsync(Guid lockId, long messageDeliveryId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); var result = await _context.QueryAsync((x, t) => x.ExecuteScalarAsync<long?>(_deleteMessageSql, new
        {
            message_delivery_id = messageDeliveryId,
            lock_id = lockId
        }), CancellationToken);

        return result == messageDeliveryId;
    }

    /// <summary>
    /// Performs the delete scheduled message operation.
    /// </summary>
    /// <param name="tokenId">The token id value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public override async Task<bool> DeleteScheduledMessageAsync(Guid tokenId, CancellationToken cancellationToken = default)
    {
        IEnumerable<SqlTransportMessage>? result = await _context.QueryAsync((x, t) => x.QueryAsync<SqlTransportMessage>(_deleteScheduledMessageSql, new
        {
            token_id = tokenId,
        }), cancellationToken);

        return result.Any();
    }

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
    public override async Task<bool> MoveMessageAsync(Guid lockId, long messageDeliveryId, string queueName, SqlQueueType queueType,
        DateTimeOffset? expirationTime, SendHeaders sendHeaders, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); IEnumerable<KeyValuePair<string, object>> headers = sendHeaders.GetAll().ToList();
        var headersAsJson = headers.Any() ? JsonSerializer.Serialize(headers, ServiceBusMetadataJson.Options) : null;

        var result = await _context.QueryAsync((x, t) => x.ExecuteScalarAsync<long?>(_moveMessageTypeSql, new
        {
            message_delivery_id = messageDeliveryId,
            lock_id = lockId,
            queue_name = queueName,
            queue_type = (int)queueType,
            expiration_time = expirationTime,
            headers = new JsonParameter(headersAsJson),
        }), CancellationToken);

        return result == messageDeliveryId;
    }

    /// <summary>
    /// Performs the renew lock operation.
    /// </summary>
    /// <param name="lockId">The lock id value.</param>
    /// <param name="messageDeliveryId">The message delivery id value.</param>
    /// <param name="duration">The duration value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public override async Task<bool> RenewLockAsync(Guid lockId, long messageDeliveryId, TimeSpan duration, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); var result = await _context.QueryAsync((x, t) => x.ExecuteScalarAsync<long?>(_renewLockSql, new
        {
            message_delivery_id = messageDeliveryId,
            lock_id = lockId,
            duration
        }), CancellationToken);

        return result == messageDeliveryId;
    }

    /// <summary>
    /// Performs the unlock operation.
    /// </summary>
    /// <param name="lockId">The lock id value.</param>
    /// <param name="messageDeliveryId">The message delivery id value.</param>
    /// <param name="delay">The delay value.</param>
    /// <param name="sendHeaders">The send headers value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public override async Task<bool> UnlockAsync(Guid lockId, long messageDeliveryId, TimeSpan delay, SendHeaders sendHeaders, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); IEnumerable<KeyValuePair<string, object>> headers = sendHeaders.GetAll().ToList();
        var headersAsJson = headers.Any() ? JsonSerializer.Serialize(headers, ServiceBusMetadataJson.Options) : null;

        var result = await _context.QueryAsync((x, t) => x.ExecuteScalarAsync<long?>(_unlockSql, new
        {
            message_delivery_id = messageDeliveryId,
            lock_id = lockId,
            delay,
            headers = new JsonParameter(headersAsJson),
        }), CancellationToken);

        return result == messageDeliveryId;
    }
}
