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

/// <summary>
/// Provides a sql server client context implementation.
/// </summary>
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

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
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

    /// <summary>
    /// Creates queue.
    /// </summary>
    /// <param name="queue">The queue value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Creates topic.
    /// </summary>
    /// <param name="topic">The topic value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public override async Task<long> CreateTopicAsync(Topic topic, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); var result = await ExecuteAsync<long>(_createTopicSql, new { topicName = topic.TopicName });

        return result ?? throw new SqlTopologyException("Create topic failed");
    }

    /// <summary>
    /// Creates topic subscription.
    /// </summary>
    /// <param name="subscription">The subscription value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Creates queue subscription.
    /// </summary>
    /// <param name="subscription">The subscription value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the purge queue operation.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public override async Task<long> PurgeQueueAsync(string queueName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); var result = await ExecuteAsync<long>(_purgeQueueSql, new { QueueName = queueName });

        return result ?? throw new SqlTopologyException("Purge queue failed");
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

    /// <summary>
    /// Performs the touch queue operation.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public override Task TouchQueueAsync(string queueName, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return ExecuteAsync<long>(_touchQueueSql, new { queueName });
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
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<int?>(cancellationToken); return ExecuteAsync<int>(_deadLetterMessagesSql, new { queueName, messageCount });
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

    /// <summary>
    /// Performs the delete message operation.
    /// </summary>
    /// <param name="lockId">The lock id value.</param>
    /// <param name="messageDeliveryId">The message delivery id value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public override async Task<bool> DeleteMessageAsync(Guid lockId, long messageDeliveryId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); var result = await ExecuteAsync<long>(_deleteMessageSql, new
        {
            messageDeliveryId,
            lockId,
        }).ConfigureAwait(false);

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
        IEnumerable<SqlTransportMessage> result = await QueryAsync<SqlTransportMessage>(_deleteScheduledMessageSql, new
        {
            tokenId,
        }, cancellationToken).ConfigureAwait(false);

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
        cancellationToken.ThrowIfCancellationRequested(); var result = await ExecuteAsync<long>(_renewMessageLockSql, new
        {
            messageDeliveryId,
            lockId,
            duration = (int)duration.TotalSeconds
        }).ConfigureAwait(false);

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
