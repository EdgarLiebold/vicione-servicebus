using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using NpgsqlTypes;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.SqlTransport.Serialization;
using ViciOne.ServiceBus.SqlTransport.Topology;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql;

/// <summary>Executes PostgreSQL queue, topic, delivery, and lock operations for one SQL transport client.</summary>
internal sealed class PostgreSqlClientContext :
    SqlClientContext
{
    readonly Guid _consumerId;
    readonly PostgreSqlDbConnectionContext _context;
    readonly string _createQueueSql;
    readonly string _createQueueSubscriptionSql;
    readonly string _createTopicSql;
    readonly string _createTopicSubscriptionSql;
    readonly string _deadLetterMessagesSql;
    readonly string _deleteMessageSql;
    readonly string _deleteScheduledMessageSql;
    readonly string _moveMessageTypeSql;
    readonly string _publishSql;
    readonly string _purgeQueueSql;
    readonly string _receivePartitionedSql;
    readonly string _receiveSql;
    readonly string _renewLockSql;
    readonly string _sendSql;
    readonly string _setExactBodySql;
    readonly string _touchQueueSql;
    readonly string _unlockSql;

    /// <summary>Initializes a client that uses the specified PostgreSQL connection context.</summary>
    /// <param name="context">The connection context used to execute transport commands.</param>
    /// <param name="cancellationToken">The token that controls the lifetime of this client context.</param>
    public PostgreSqlClientContext(PostgreSqlDbConnectionContext context, CancellationToken cancellationToken)
        : base(context, cancellationToken)
    {
        _context = context;
        _consumerId = NewId.NextGuid();

        _createQueueSubscriptionSql = string.Format(PostgreSqlStatements.DbCreateQueueSubscriptionSql, _context.Schema);
        _receiveSql = string.Format(PostgreSqlStatements.DbReceiveSql, _context.Schema);
        _receivePartitionedSql = string.Format(PostgreSqlStatements.DbReceivePartitionedSql, _context.Schema);
        _sendSql = string.Format(PostgreSqlStatements.DbEnqueueSql, _context.Schema);
        _setExactBodySql = string.Format(PostgreSqlStatements.DbSetExactBodySql, _context.Schema);
        _createTopicSubscriptionSql = string.Format(PostgreSqlStatements.DbCreateTopicSubscriptionSql, _context.Schema);
        _publishSql = string.Format(PostgreSqlStatements.DbPublishSql, _context.Schema);
        _purgeQueueSql = string.Format(PostgreSqlStatements.DbPurgeQueueSql, _context.Schema);
        _deadLetterMessagesSql = string.Format(PostgreSqlStatements.DbDeadLetterMessagesSql, _context.Schema);
        _createTopicSql = string.Format(PostgreSqlStatements.DbCreateTopicSql, _context.Schema);
        _createQueueSql = string.Format(PostgreSqlStatements.DbCreateQueueSql, _context.Schema);
        _deleteMessageSql = string.Format(PostgreSqlStatements.DbDeleteMessageSql, _context.Schema);
        _deleteScheduledMessageSql = string.Format(PostgreSqlStatements.DbDeleteScheduledMessageSql, _context.Schema);
        _moveMessageTypeSql = string.Format(PostgreSqlStatements.DbMoveMessageSql, _context.Schema);
        _renewLockSql = string.Format(PostgreSqlStatements.DbRenewLockSql, _context.Schema);
        _touchQueueSql = string.Format(PostgreSqlStatements.DbTouchQueueSql, _context.Schema);
        _unlockSql = string.Format(PostgreSqlStatements.DbUnlockSql, _context.Schema);
    }

    /// <summary>Creates or resolves a transport queue.</summary>
    /// <param name="queue">The queue topology definition.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The database identifier of the queue.</returns>
    public override Task<long> CreateQueueAsync(Queue queue, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queue);

        return ExecuteDatabaseOperationAsync((connection, transaction, token) =>
        {
            var command = new CommandDefinition(_createQueueSql, new
            {
                queue_name = queue.QueueName,
                auto_delete = SqlTransportDefaults.ToDatabaseAutoDeleteSeconds(queue.AutoDeleteOnIdle),
                max_delivery_count = queue.MaxDeliveryCount
            }, transaction, cancellationToken: token);

            return connection.ExecuteScalarAsync<long>(command);
        }, cancellationToken);
    }

    /// <summary>Creates or resolves a transport topic.</summary>
    /// <param name="topic">The topic topology definition.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The database identifier of the topic.</returns>
    public override Task<long> CreateTopicAsync(Topic topic, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(topic);

        return ExecuteDatabaseOperationAsync((connection, transaction, token) =>
        {
            var command = new CommandDefinition(_createTopicSql, new { topic_name = topic.TopicName }, transaction, cancellationToken: token);

            return connection.ExecuteScalarAsync<long>(command);
        }, cancellationToken);
    }

    /// <summary>Creates or resolves a topic-to-topic subscription.</summary>
    /// <param name="subscription">The subscription topology definition.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The database identifier of the subscription.</returns>
    public override Task<long> CreateTopicSubscriptionAsync(TopicToTopicSubscription subscription, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subscription);

        return ExecuteDatabaseOperationAsync((connection, transaction, token) =>
        {
            var command = new CommandDefinition(_createTopicSubscriptionSql, new
            {
                source_topic_name = subscription.Source.TopicName,
                destination_topic_name = subscription.Destination.TopicName,
                type = (int)subscription.SubscriptionType,
                routing_key = subscription.RoutingKey,
                filter = new JsonParameter(null)
            }, transaction, cancellationToken: token);

            return connection.ExecuteScalarAsync<long>(command);
        }, cancellationToken);
    }

    /// <summary>Creates or resolves a topic-to-queue subscription.</summary>
    /// <param name="subscription">The subscription topology definition.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The database identifier of the subscription.</returns>
    public override Task<long> CreateQueueSubscriptionAsync(TopicToQueueSubscription subscription, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subscription);

        return ExecuteDatabaseOperationAsync((connection, transaction, token) =>
        {
            var command = new CommandDefinition(_createQueueSubscriptionSql, new
            {
                source_topic_name = subscription.Source.TopicName,
                destination_queue_name = subscription.Destination.QueueName,
                type = (int)subscription.SubscriptionType,
                routing_key = subscription.RoutingKey,
                filter = new JsonParameter(null)
            }, transaction, cancellationToken: token);

            return connection.ExecuteScalarAsync<long>(command);
        }, cancellationToken);
    }

    /// <summary>Removes currently available messages from a queue.</summary>
    /// <param name="queueName">The queue to purge.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The purge result reported by PostgreSQL.</returns>
    public override Task<long> PurgeQueueAsync(string queueName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);

        return ExecuteDatabaseOperationAsync((connection, transaction, token) =>
        {
            var command = new CommandDefinition(_purgeQueueSql, new { queue_name = queueName }, transaction, cancellationToken: token);

            return connection.ExecuteScalarAsync<long>(command);
        }, cancellationToken);
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
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);

        try
        {
            if (mode == SqlReceiveMode.Normal)
            {
                return await ExecuteDatabaseOperationAsync((connection, transaction, token) =>
                {
                    var command = new CommandDefinition(_receiveSql, new
                    {
                        queue_name = queueName,
                        fetch_consumer_id = _consumerId,
                        fetch_lock_id = NewId.NextGuid(),
                        lock_duration = lockDuration,
                        fetch_count = messageLimit
                    }, transaction, cancellationToken: token);

                    return connection.QueryAsync<SqlTransportMessage>(command);
                }, cancellationToken).ConfigureAwait(false);
            }

            var ordered = mode switch
            {
                SqlReceiveMode.PartitionedOrdered => 1,
                SqlReceiveMode.PartitionedOrderedConcurrent => 1,
                _ => 0
            };

            return await ExecuteDatabaseOperationAsync((connection, transaction, token) =>
            {
                var command = new CommandDefinition(_receivePartitionedSql, new
                {
                    queue_name = queueName,
                    fetch_consumer_id = _consumerId,
                    fetch_lock_id = NewId.NextGuid(),
                    lock_duration = lockDuration,
                    fetch_count = messageLimit,
                    concurrent_count = concurrentLimit,
                    ordered
                }, transaction, cancellationToken: token);

                return connection.QueryAsync<SqlTransportMessage>(command);
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.SerializationFailure)
        {
            return [];
        }
    }

    /// <summary>Updates a queue's last-used timestamp.</summary>
    /// <param name="queueName">The queue to mark as used.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes after PostgreSQL updates the queue timestamp.</returns>
    public override Task TouchQueueAsync(string queueName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);

        return ExecuteDatabaseOperationAsync((connection, transaction, token) =>
        {
            var command = new CommandDefinition(_touchQueueSql, new { queue_name = queueName }, transaction, cancellationToken: token);

            return connection.ExecuteScalarAsync<int?>(command);
        }, cancellationToken);
    }

    /// <summary>Moves eligible messages from a queue to its dead-letter queue.</summary>
    /// <param name="queueName">The source queue.</param>
    /// <param name="messageCount">The maximum number of messages to move.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The number of messages moved, or <see langword="null" /> when the operation produces no result.</returns>
    public override Task<int?> DeadLetterQueueAsync(string queueName, int messageCount, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);

        return ExecuteDatabaseOperationAsync((connection, transaction, token) =>
        {
            var command = new CommandDefinition(_deadLetterMessagesSql, new
            {
                queue_name = queueName,
                message_count = messageCount
            }, transaction, cancellationToken: token);

            return connection.ExecuteScalarAsync<int?>(command);
        }, cancellationToken);
    }

    /// <summary>Enqueues a message and its transport metadata in a queue.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="queueName">The destination queue.</param>
    /// <param name="context">The serialized message and send metadata.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes after PostgreSQL enqueues the message.</returns>
    public override Task SendAsync<T>(string queueName, SqlMessageSendContext<T> context, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        ArgumentNullException.ThrowIfNull(context);

        IEnumerable<KeyValuePair<string, object>> headers = context.Headers.GetAll().ToList();
        var headersAsJson = headers.Any() ? JsonSerializer.Serialize(headers, ServiceBusMetadataJson.Options) : null;

        Guid? schedulingTokenId = context.Headers.Get<Guid>(MessageHeaders.SchedulingTokenId);
        DateTime? expirationTime = context.TimeToLive.HasValue
            ? context.GetTimeProvider().GetUtcNow().UtcDateTime + context.TimeToLive.Value
            : null;
        SqlMessageBodyStorage bodyStorage = SqlMessageBodyStorage.Create(context);
        string? exactBody = bodyStorage.Text;

        return ExecuteDatabaseOperationAsync(async (connection, transaction, token) =>
        {
            var command = new CommandDefinition(_sendSql, new
            {
                entity_name = queueName,
                priority = (int)(context.Priority ?? 100),
                transport_message_id = context.TransportMessageId,
                body = new JsonParameter(null),
                body_exact = new JsonParameter(exactBody, NpgsqlDbType.Json),
                binary_body = bodyStorage.Binary,
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
            }, transaction, cancellationToken: token);

            long? sent = await connection.ExecuteScalarAsync<long?>(command).ConfigureAwait(false);
            if (exactBody is not null)
            {
                if (sent != 1)
                    throw new InvalidOperationException("A PostgreSQL send did not persist its transport message.");

                await SetExactBodyAsync(connection, transaction, context.TransportMessageId, exactBody, token).ConfigureAwait(false);
            }

            return sent;
        }, cancellationToken);
    }

    /// <summary>Publishes a message and its transport metadata through a topic.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="topicName">The source topic used to resolve subscriptions.</param>
    /// <param name="context">The serialized message and publish metadata.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes after PostgreSQL publishes the message to matching subscriptions.</returns>
    public override Task PublishAsync<T>(string topicName, SqlMessageSendContext<T> context, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topicName);
        ArgumentNullException.ThrowIfNull(context);

        IEnumerable<KeyValuePair<string, object>> headers = context.Headers.GetAll().ToList();
        var headersAsJson = headers.Any() ? JsonSerializer.Serialize(headers, ServiceBusMetadataJson.Options) : null;

        Guid? schedulingTokenId = context.Headers.Get<Guid>(MessageHeaders.SchedulingTokenId);
        DateTime? expirationTime = context.TimeToLive.HasValue
            ? context.GetTimeProvider().GetUtcNow().UtcDateTime + context.TimeToLive.Value
            : null;
        SqlMessageBodyStorage bodyStorage = SqlMessageBodyStorage.Create(context);
        string? exactBody = bodyStorage.Text;

        return ExecuteDatabaseOperationAsync(async (connection, transaction, token) =>
        {
            var command = new CommandDefinition(_publishSql, new
            {
                entity_name = topicName,
                priority = (int)(context.Priority ?? 100),
                transport_message_id = context.TransportMessageId,
                body = new JsonParameter(null),
                body_exact = new JsonParameter(exactBody, NpgsqlDbType.Json),
                binary_body = bodyStorage.Binary,
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
            }, transaction, cancellationToken: token);

            long? published = await connection.ExecuteScalarAsync<long?>(command).ConfigureAwait(false);
            if (exactBody is not null)
            {
                if (published is null)
                    throw new InvalidOperationException("A PostgreSQL publish did not return a recipient count.");

                if (published > 0)
                    await SetExactBodyAsync(connection, transaction, context.TransportMessageId, exactBody, token).ConfigureAwait(false);
            }

            return published;
        }, cancellationToken);
    }

    async Task SetExactBodyAsync(IDbConnection connection, IDbTransaction transaction, Guid transportMessageId, string body, CancellationToken cancellationToken)
    {
        var command = new CommandDefinition(_setExactBodySql, new
        {
            transport_message_id = transportMessageId,
            body_exact = new JsonParameter(body, NpgsqlDbType.Json)
        }, transaction, cancellationToken: cancellationToken);

        if (await connection.ExecuteAsync(command).ConfigureAwait(false) != 1)
            throw new InvalidOperationException("A PostgreSQL message body was not persisted exactly.");
    }

    /// <summary>Deletes a delivered message when the supplied lock still owns it.</summary>
    /// <param name="lockId">The current delivery lock identifier.</param>
    /// <param name="messageDeliveryId">The delivery identifier.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns><see langword="true" /> when the delivery was deleted; otherwise, <see langword="false" />.</returns>
    public override async Task<bool> DeleteMessageAsync(Guid lockId, long messageDeliveryId, CancellationToken cancellationToken = default)
    {
        var result = await ExecuteDatabaseOperationAsync((connection, transaction, token) =>
        {
            var command = new CommandDefinition(_deleteMessageSql, new
            {
                message_delivery_id = messageDeliveryId,
                lock_id = lockId
            }, transaction, cancellationToken: token);

            return connection.ExecuteScalarAsync<long?>(command);
        }, cancellationToken).ConfigureAwait(false);

        return result == messageDeliveryId;
    }

    /// <summary>Deletes a scheduled message identified by its scheduling token.</summary>
    /// <param name="tokenId">The scheduling token identifier.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns><see langword="true" /> when a scheduled message was deleted; otherwise, <see langword="false" />.</returns>
    public override async Task<bool> DeleteScheduledMessageAsync(Guid tokenId, CancellationToken cancellationToken = default)
    {
        IEnumerable<SqlTransportMessage> result = await ExecuteDatabaseOperationAsync((connection, transaction, token) =>
        {
            var command = new CommandDefinition(_deleteScheduledMessageSql, new { token_id = tokenId }, transaction, cancellationToken: token);

            return connection.QueryAsync<SqlTransportMessage>(command);
        }, cancellationToken).ConfigureAwait(false);

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
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        ArgumentNullException.ThrowIfNull(sendHeaders);

        IEnumerable<KeyValuePair<string, object>> headers = sendHeaders.GetAll().ToList();
        var headersAsJson = headers.Any() ? JsonSerializer.Serialize(headers, ServiceBusMetadataJson.Options) : null;

        var result = await ExecuteDatabaseOperationAsync((connection, transaction, token) =>
        {
            var command = new CommandDefinition(_moveMessageTypeSql, new
            {
                message_delivery_id = messageDeliveryId,
                lock_id = lockId,
                queue_name = queueName,
                queue_type = (int)queueType,
                expiration_time = expirationTime,
                headers = new JsonParameter(headersAsJson)
            }, transaction, cancellationToken: token);

            return connection.ExecuteScalarAsync<long?>(command);
        }, cancellationToken).ConfigureAwait(false);

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
        var result = await ExecuteDatabaseOperationAsync((connection, transaction, token) =>
        {
            var command = new CommandDefinition(_renewLockSql, new
            {
                message_delivery_id = messageDeliveryId,
                lock_id = lockId,
                duration
            }, transaction, cancellationToken: token);

            return connection.ExecuteScalarAsync<long?>(command);
        }, cancellationToken).ConfigureAwait(false);

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
        ArgumentNullException.ThrowIfNull(sendHeaders);

        IEnumerable<KeyValuePair<string, object>> headers = sendHeaders.GetAll().ToList();
        var headersAsJson = headers.Any() ? JsonSerializer.Serialize(headers, ServiceBusMetadataJson.Options) : null;

        var result = await ExecuteDatabaseOperationAsync((connection, transaction, token) =>
        {
            var command = new CommandDefinition(_unlockSql, new
            {
                message_delivery_id = messageDeliveryId,
                lock_id = lockId,
                delay,
                headers = new JsonParameter(headersAsJson)
            }, transaction, cancellationToken: token);

            return connection.ExecuteScalarAsync<long?>(command);
        }, cancellationToken).ConfigureAwait(false);

        return result == messageDeliveryId;
    }

}
