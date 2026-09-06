using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.SqlTransport.Topology;

namespace ViciOne.ServiceBus.SqlTransport.SqlServer;

/// <summary>Executes SQL Server queue, topic, delivery, and lock operations for one SQL transport client.</summary>
public class SqlServerClientContext :
    SqlClientContext
{
    readonly Guid _consumerId;
    readonly SqlServerDbConnectionContext _context;
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
    readonly string _renewMessageLockSql;
    readonly string _sendSql;
    readonly string _touchQueueSql;
    readonly string _unlockSql;
    readonly string _deadLetterMessagesSql;

    /// <summary>Initializes a client that uses the specified SQL Server connection context.</summary>
    /// <param name="context">The connection context used to execute transport commands.</param>
    /// <param name="cancellationToken">The token that controls the lifetime of this client context.</param>
    public SqlServerClientContext(SqlServerDbConnectionContext context, CancellationToken cancellationToken)
        : base(context, cancellationToken)
    {
        _context = context;
        _consumerId = NewId.NextGuid();

        _createQueueSql = $"{_context.Schema}.CreateQueueV2";
        _createTopicSql = $"{_context.Schema}.CreateTopic";
        _createTopicSubscriptionSql = $"{_context.Schema}.CreateTopicSubscription";
        _createQueueSubscriptionSql = $"{_context.Schema}.CreateQueueSubscription";
        _sendSql = $"{_context.Schema}.SendMessageV2";
        _publishSql = $"{_context.Schema}.PublishMessageV2";
        _purgeQueueSql = $"{_context.Schema}.PurgeQueue";
        _deadLetterMessagesSql = $"{_context.Schema}.DeadLetterMessages";
        _receiveSql = $"{_context.Schema}.FetchMessages";
        _receivePartitionedSql = $"{_context.Schema}.FetchMessagesPartitioned";
        _deleteMessageSql = $"{_context.Schema}.DeleteMessage";
        _renewMessageLockSql = $"{_context.Schema}.RenewMessageLock";
        _touchQueueSql = $"{_context.Schema}.TouchQueue";
        _unlockSql = $"{_context.Schema}.UnlockMessage";
        _moveMessageTypeSql = $"{_context.Schema}.MoveMessage";
        _deleteScheduledMessageSql = $"{_context.Schema}.DeleteScheduledMessage";
    }

    /// <summary>Creates or resolves a transport queue.</summary>
    /// <param name="queue">The queue topology definition.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The database identifier of the queue.</returns>
    public override async Task<long> CreateQueueAsync(Queue queue, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); var result = await ExecuteAsync<long>(_createQueueSql, new
        {
            queueName = queue.QueueName,
            autoDelete = (int?)queue.AutoDeleteOnIdle?.TotalSeconds,
            maxDeliveryCount = queue.MaxDeliveryCount
        });

        return result ?? throw new SqlTopologyException("Create queue failed");
    }

    /// <summary>Creates or resolves a transport topic.</summary>
    /// <param name="topic">The topic topology definition.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The database identifier of the topic.</returns>
    public override async Task<long> CreateTopicAsync(Topic topic, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); var result = await ExecuteAsync<long>(_createTopicSql, new { topicName = topic.TopicName });

        return result ?? throw new SqlTopologyException("Create topic failed");
    }

    /// <summary>Creates or resolves a topic-to-topic subscription.</summary>
    /// <param name="subscription">The subscription topology definition.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The database identifier of the subscription.</returns>
    public override async Task<long> CreateTopicSubscriptionAsync(TopicToTopicSubscription subscription, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); var result = await ExecuteAsync<long>(_createTopicSubscriptionSql, new
        {
            SourceTopicName = subscription.Source.TopicName,
            DestinationTopicName = subscription.Destination.TopicName,
            SubscriptionType = (int)subscription.SubscriptionType,
            RoutingKey = subscription.RoutingKey ?? "",
            Filter = "{}"
        });

        return result ?? throw new SqlTopologyException("Create topic subscription failed");
    }

    /// <summary>Creates or resolves a topic-to-queue subscription.</summary>
    /// <param name="subscription">The subscription topology definition.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The database identifier of the subscription.</returns>
    public override async Task<long> CreateQueueSubscriptionAsync(TopicToQueueSubscription subscription, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); var result = await ExecuteAsync<long>(_createQueueSubscriptionSql, new
        {
            SourceTopicName = subscription.Source.TopicName,
            DestinationQueueName = subscription.Destination.QueueName,
            SubscriptionType = (int)subscription.SubscriptionType,
            RoutingKey = subscription.RoutingKey ?? "",
            Filter = "{}"
        });

        return result ?? throw new SqlTopologyException("Create queue subscription failed");
    }

    /// <summary>Removes currently available messages from a queue.</summary>
    /// <param name="queueName">The queue to purge.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The number of messages removed.</returns>
    public override async Task<long> PurgeQueueAsync(string queueName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); var result = await ExecuteAsync<long>(_purgeQueueSql, new { QueueName = queueName });

        return result ?? throw new SqlTopologyException("Purge queue failed");
    }

    /// <summary>Acquires and returns a batch of messages from a queue.</summary>
    /// <param name="queueName">The queue from which messages are acquired.</param>
    /// <param name="mode">The receive ordering and partition mode.</param>
    /// <param name="messageLimit">The maximum number of messages to return.</param>
    /// <param name="concurrentLimit">The maximum number of concurrently active partitions for a partitioned receive.</param>
    /// <param name="lockDuration">How long acquired messages remain locked.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The acquired transport messages, or an empty sequence when SQL Server selects this transaction as a deadlock victim.</returns>
    public override async Task<IEnumerable<SqlTransportMessage>> ReceiveMessagesAsync(string queueName, SqlReceiveMode mode, int messageLimit,
        int concurrentLimit, TimeSpan lockDuration, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); try
        {
            if (mode == SqlReceiveMode.Normal)
            {
                return await QueryAsync<SqlTransportMessage>(_receiveSql, new
                {
                    queueName,
                    consumerId = _consumerId,
                    lockId = NewId.NextGuid(),
                    lockDuration = (int)lockDuration.TotalSeconds,
                    fetchCount = messageLimit
                }).ConfigureAwait(false);
            }

            var ordered = mode switch
            {
                SqlReceiveMode.PartitionedOrdered => 1,
                SqlReceiveMode.PartitionedOrderedConcurrent => 1,
                _ => 0
            };

            return await QueryAsync<SqlTransportMessage>(_receivePartitionedSql, new
            {
                queueName,
                consumerId = _consumerId,
                lockId = NewId.NextGuid(),
                lockDuration = (int)lockDuration.TotalSeconds,
                fetchCount = messageLimit,
                concurrentCount = concurrentLimit,
                ordered
            }).ConfigureAwait(false);
        }
        catch (SqlException exception) when (exception.Number == 1205)
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
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return ExecuteAsync<long>(_touchQueueSql, new { queueName });
    }

    /// <summary>Moves eligible messages from a queue to its dead-letter queue.</summary>
    /// <param name="queueName">The source queue.</param>
    /// <param name="messageCount">The maximum number of messages to move.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The number of messages moved, or <see langword="null" /> when the operation produces no result.</returns>
    public override Task<int?> DeadLetterQueueAsync(string queueName, int messageCount, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<int?>(cancellationToken); return ExecuteAsync<int>(_deadLetterMessagesSql, new { queueName, messageCount });
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

        return ExecuteAsync<long>(_sendSql, new
        {
            entityName = queueName,
            priority = (int)(context.Priority ?? 100),
            transportMessageId = context.TransportMessageId,
            body = context.Body.GetString(),
            binaryBody = default(byte[]?),
            contentType = context.ContentType?.MediaType,
            messageType = string.Join(";", context.SupportedMessageTypes),
            messageId = context.MessageId,
            correlationId = context.CorrelationId,
            conversationId = context.ConversationId,
            requestId = context.RequestId,
            initiatorId = context.InitiatorId,
            sourceAddress = context.SourceAddress,
            destinationAddress = context.DestinationAddress,
            responseAddress = context.ResponseAddress,
            faultAddress = context.FaultAddress,
            sentTime = context.SentTime,
            expirationTime,
            headers = headersAsJson,
            host = HostInfoCache.HostInfoJson,
            partitionKey = context.PartitionKey,
            routingKey = context.RoutingKey,
            delay = (int?)context.Delay?.TotalSeconds,
            schedulingTokenId
        });
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

        return ExecuteAsync<long>(_publishSql, new
        {
            entityName = topicName,
            priority = (int)(context.Priority ?? 100),
            transportMessageId = context.TransportMessageId,
            body = context.Body.GetString(),
            binaryBody = default(byte[]?),
            contentType = context.ContentType?.MediaType,
            messageType = string.Join(";", context.SupportedMessageTypes),
            messageId = context.MessageId,
            correlationId = context.CorrelationId,
            conversationId = context.ConversationId,
            requestId = context.RequestId,
            initiatorId = context.InitiatorId,
            sourceAddress = context.SourceAddress,
            destinationAddress = context.DestinationAddress,
            responseAddress = context.ResponseAddress,
            faultAddress = context.FaultAddress,
            sentTime = context.SentTime,
            expirationTime,
            headers = headersAsJson,
            host = HostInfoCache.HostInfoJson,
            partitionKey = context.PartitionKey,
            routingKey = context.RoutingKey,
            delay = (int?)context.Delay?.TotalSeconds,
            schedulingTokenId
        });
    }

    /// <summary>Deletes a delivered message when the supplied lock still owns it.</summary>
    /// <param name="lockId">The current delivery lock identifier.</param>
    /// <param name="messageDeliveryId">The delivery identifier.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns><see langword="true" /> when the delivery was deleted; otherwise, <see langword="false" />.</returns>
    public override async Task<bool> DeleteMessageAsync(Guid lockId, long messageDeliveryId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); var result = await ExecuteAsync<long>(_deleteMessageSql, new
        {
            messageDeliveryId,
            lockId,
        }).ConfigureAwait(false);

        return result == messageDeliveryId;
    }

    /// <summary>Deletes a scheduled message identified by its scheduling token.</summary>
    /// <param name="tokenId">The scheduling token identifier.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns><see langword="true" /> when a scheduled message was deleted; otherwise, <see langword="false" />.</returns>
    public override async Task<bool> DeleteScheduledMessageAsync(Guid tokenId, CancellationToken cancellationToken = default)
    {
        IEnumerable<SqlTransportMessage> result = await QueryAsync<SqlTransportMessage>(_deleteScheduledMessageSql, new
        {
            tokenId,
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
        cancellationToken.ThrowIfCancellationRequested(); IEnumerable<KeyValuePair<string, object>> headers = sendHeaders.GetAll().ToList();
        var headersAsJson = headers.Any() ? JsonSerializer.Serialize(headers, ServiceBusMetadataJson.Options) : null;

        var result = await ExecuteAsync<long>(_moveMessageTypeSql, new
        {
            messageDeliveryId,
            lockId,
            queueName,
            queueType,
            expirationTime,
            headers = headersAsJson
        }).ConfigureAwait(false);

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
        cancellationToken.ThrowIfCancellationRequested(); var result = await ExecuteAsync<long>(_renewMessageLockSql, new
        {
            messageDeliveryId,
            lockId,
            duration = (int)duration.TotalSeconds
        }).ConfigureAwait(false);

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

        var result = await ExecuteAsync<long>(_unlockSql, new
        {
            messageDeliveryId,
            lockId,
            delay = delay > TimeSpan.Zero ? Math.Max((int)delay.TotalSeconds, 1) : 0,
            headers = headersAsJson
        }).ConfigureAwait(false);

        return result == messageDeliveryId;
    }

    Task<T?> ExecuteAsync<T>(string functionName, object values)
        where T : struct
    {
        return _context.QueryAsync((connection, transaction) => connection
            .ExecuteScalarAsync<T?>(functionName, values, transaction, commandType: CommandType.StoredProcedure), CancellationToken);
    }

    Task<T?> QuerySingleAsync<T>(string functionName, object values)
        where T : class
    {
        return _context.QueryAsync((connection, transaction) => connection
            .QuerySingleAsync<T?>(functionName, values, transaction, commandType: CommandType.StoredProcedure), CancellationToken);
    }

    Task<IEnumerable<T>> QueryAsync<T>(string functionName, object values)
        where T : class
    {
        return _context.QueryAsync((connection, transaction) => connection
            .QueryAsync<T>(functionName, values, transaction, commandType: CommandType.StoredProcedure), CancellationToken);
    }

    Task<IEnumerable<T>> QueryAsync<T>(string functionName, object values, CancellationToken cancellationToken)
        where T : class
    {
        return _context.QueryAsync((connection, transaction) => connection
            .QueryAsync<T>(functionName, values, transaction, commandType: CommandType.StoredProcedure), cancellationToken);
    }
}
