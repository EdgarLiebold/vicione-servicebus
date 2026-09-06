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

/// <summary>Executes PostgreSQL queue, topic, delivery, and lock operations for one SQL transport client.</summary>
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

    /// <summary>Initializes a client that uses the specified PostgreSQL connection context.</summary>
    /// <param name="context">The connection context used to execute transport commands.</param>
    /// <param name="cancellationToken">The token that controls the lifetime of this client context.</param>
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

    /// <summary>Creates or resolves a transport queue.</summary>
    /// <param name="queue">The queue topology definition.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The database identifier of the queue.</returns>
    public override Task<long> CreateQueueAsync(Queue queue, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<long>(cancellationToken); return _context.QueryAsync((x, t) => x.ExecuteScalarAsync<long>(_createQueueSql, new
        {
            queue_name = queue.QueueName,
            auto_delete = (int?)queue.AutoDeleteOnIdle?.TotalSeconds,
            max_delivery_count = queue.MaxDeliveryCount
        }, t), CancellationToken);
    }

    /// <summary>Creates or resolves a transport topic.</summary>
    /// <param name="topic">The topic topology definition.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The database identifier of the topic.</returns>
    public override Task<long> CreateTopicAsync(Topic topic, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<long>(cancellationToken); return _context.QueryAsync((x, t) => x.ExecuteScalarAsync<long>(_createTopicSql, new { topic_name = topic.TopicName }), CancellationToken);
    }

    /// <summary>Creates or resolves a topic-to-topic subscription.</summary>
    /// <param name="subscription">The subscription topology definition.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The database identifier of the subscription.</returns>
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

    /// <summary>Creates or resolves a topic-to-queue subscription.</summary>
    /// <param name="subscription">The subscription topology definition.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The database identifier of the subscription.</returns>
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

    /// <summary>Removes currently available messages from a queue.</summary>
    /// <param name="queueName">The queue to purge.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The purge result reported by PostgreSQL.</returns>
    public override Task<long> PurgeQueueAsync(string queueName, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<long>(cancellationToken); return _context.QueryAsync((x, t) => x.ExecuteScalarAsync<long>(_purgeQueueSql, new { queue_name = queueName }), CancellationToken);
    }

    /// <summary>Acquires and returns a batch of messages from a queue.</summary>
    /// <param name="queueName">The queue from which messages are acquired.</param>
    /// <param name="mode">The receive ordering and partition mode.</param>
    /// <param name="messageLimit">The maximum number of messages to return.</param>
    /// <param name="concurrentLimit">The maximum number of concurrently active partitions for a partitioned receive.</param>
    /// <param name="lockDuration">How long acquired messages remain locked.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The acquired transport messages, or an empty sequence when PostgreSQL reports a serialization conflict.</returns>
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

    /// <summary>Updates a queue's last-used timestamp.</summary>
    /// <param name="queueName">The queue to mark as used.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public override Task TouchQueueAsync(string queueName, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return _context.QueryAsync((x, t) => x.ExecuteScalarAsync<int?>(_touchQueueSql, new { queue_name = queueName }), CancellationToken);
    }

    /// <summary>Moves eligible messages from a queue to its dead-letter queue.</summary>
    /// <param name="queueName">The source queue.</param>
    /// <param name="messageCount">The maximum number of messages to move.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The number of messages moved, or <see langword="null" /> when the operation produces no result.</returns>
    public override Task<int?> DeadLetterQueueAsync(string queueName, int messageCount, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<int?>(cancellationToken); return _context.QueryAsync((x, t) => x.ExecuteScalarAsync<int?>(_deadLetterMessagesSql, new
        {
            queue_name = queueName,
            message_count = messageCount
        }), CancellationToken);
    }

    /// <summary>Enqueues a message and its transport metadata in a queue.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="queueName">The destination queue.</param>
    /// <param name="context">The serialized message and send metadata.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

    /// <summary>Publishes a message and its transport metadata through a topic.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="topicName">The source topic used to resolve subscriptions.</param>
    /// <param name="context">The serialized message and publish metadata.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

    /// <summary>Deletes a delivered message when the supplied lock still owns it.</summary>
    /// <param name="lockId">The current delivery lock identifier.</param>
    /// <param name="messageDeliveryId">The delivery identifier.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns><see langword="true" /> when the delivery was deleted; otherwise, <see langword="false" />.</returns>
    public override async Task<bool> DeleteMessageAsync(Guid lockId, long messageDeliveryId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); var result = await _context.QueryAsync((x, t) => x.ExecuteScalarAsync<long?>(_deleteMessageSql, new
        {
            message_delivery_id = messageDeliveryId,
            lock_id = lockId
        }), CancellationToken);

        return result == messageDeliveryId;
    }

    /// <summary>Deletes a scheduled message identified by its scheduling token.</summary>
    /// <param name="tokenId">The scheduling token identifier.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns><see langword="true" /> when a scheduled message was deleted; otherwise, <see langword="false" />.</returns>
    public override async Task<bool> DeleteScheduledMessageAsync(Guid tokenId, CancellationToken cancellationToken = default)
    {
        IEnumerable<SqlTransportMessage>? result = await _context.QueryAsync((x, t) => x.QueryAsync<SqlTransportMessage>(_deleteScheduledMessageSql, new
        {
            token_id = tokenId,
        }), cancellationToken);

        return result.Any();
    }

    /// <summary>Moves a locked delivery to a queue and applies replacement transport metadata.</summary>
    /// <param name="lockId">The current delivery lock identifier.</param>
    /// <param name="messageDeliveryId">The delivery identifier.</param>
    /// <param name="queueName">The destination queue.</param>
    /// <param name="queueType">The destination queue category used for transport metrics.</param>
    /// <param name="expirationTime">The replacement expiration timestamp, or <see langword="null" /> for no expiration.</param>
    /// <param name="sendHeaders">Headers to merge into the moved message.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns><see langword="true" /> when the delivery was moved; otherwise, <see langword="false" />.</returns>
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

    /// <summary>Extends the lock on a delivery owned by the supplied lock identifier.</summary>
    /// <param name="lockId">The current delivery lock identifier.</param>
    /// <param name="messageDeliveryId">The delivery identifier.</param>
    /// <param name="duration">The additional lock duration measured from the database clock.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns><see langword="true" /> when the lock was renewed; otherwise, <see langword="false" />.</returns>
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

    /// <summary>Releases a delivery lock and makes the message available after an optional delay.</summary>
    /// <param name="lockId">The current delivery lock identifier.</param>
    /// <param name="messageDeliveryId">The delivery identifier.</param>
    /// <param name="delay">The delay before the message becomes available again.</param>
    /// <param name="sendHeaders">Headers to merge into the unlocked message.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns><see langword="true" /> when the delivery was unlocked; otherwise, <see langword="false" />.</returns>
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
