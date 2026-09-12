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
using ViciOne.ServiceBus.SqlTransport.Serialization;
using ViciOne.ServiceBus.SqlTransport.Topology;

namespace ViciOne.ServiceBus.SqlTransport.SqlServer;

/// <summary>Executes SQL Server queue, topic, delivery, and lock operations for one SQL transport client.</summary>
internal sealed class SqlServerClientContext :
    SqlClientContext
{
    readonly Guid _consumerId;
    readonly SqlServerConnectionContext _context;
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
    readonly string _renewMessageLockSql;
    readonly string _sendSql;
    readonly string _touchQueueSql;
    readonly string _unlockSql;

    /// <summary>Initializes a client that uses the specified SQL Server connection context.</summary>
    /// <param name="context">The connection context used to execute transport commands.</param>
    /// <param name="cancellationToken">The token that controls the lifetime of this client context.</param>
    public SqlServerClientContext(SqlServerConnectionContext context, CancellationToken cancellationToken)
        : base(context ?? throw new ArgumentNullException(nameof(context)), cancellationToken)
    {
        _context = context;
        _consumerId = NewId.NextGuid();

        _createQueueSql = $"{_context.Schema}.CreateQueue";
        _createTopicSql = $"{_context.Schema}.CreateTopic";
        _createTopicSubscriptionSql = $"{_context.Schema}.CreateTopicSubscription";
        _createQueueSubscriptionSql = $"{_context.Schema}.CreateQueueSubscription";
        _sendSql = $"{_context.Schema}.SendMessage";
        _publishSql = $"{_context.Schema}.PublishMessage";
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
        ArgumentNullException.ThrowIfNull(queue);

        long? result = await ExecuteScalarAsync<long>(_createQueueSql, new
        {
            queueName = queue.QueueName,
            autoDelete = (int?)queue.AutoDeleteOnIdle?.TotalSeconds,
            maxDeliveryCount = queue.MaxDeliveryCount
        }, cancellationToken).ConfigureAwait(false);

        return result ?? throw new SqlTopologyException($"SQL Server did not return an identifier for queue '{queue.QueueName}'.");
    }

    /// <summary>Creates or resolves a transport topic.</summary>
    /// <param name="topic">The topic topology definition.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The database identifier of the topic.</returns>
    public override async Task<long> CreateTopicAsync(Topic topic, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(topic);

        long? result = await ExecuteScalarAsync<long>(
            _createTopicSql,
            new { topicName = topic.TopicName },
            cancellationToken).ConfigureAwait(false);

        return result ?? throw new SqlTopologyException($"SQL Server did not return an identifier for topic '{topic.TopicName}'.");
    }

    /// <summary>Creates or resolves a topic-to-topic subscription.</summary>
    /// <param name="subscription">The subscription topology definition.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The database identifier of the subscription.</returns>
    public override async Task<long> CreateTopicSubscriptionAsync(TopicToTopicSubscription subscription, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subscription);

        long? result = await ExecuteScalarAsync<long>(_createTopicSubscriptionSql, new
        {
            SourceTopicName = subscription.Source.TopicName,
            DestinationTopicName = subscription.Destination.TopicName,
            SubscriptionType = (int)subscription.SubscriptionType,
            RoutingKey = subscription.RoutingKey ?? string.Empty,
            Filter = "{}"
        }, cancellationToken).ConfigureAwait(false);

        return result ?? throw new SqlTopologyException(
            $"SQL Server did not return an identifier for subscription '{subscription.Source.TopicName}' to '{subscription.Destination.TopicName}'.");
    }

    /// <summary>Creates or resolves a topic-to-queue subscription.</summary>
    /// <param name="subscription">The subscription topology definition.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The database identifier of the subscription.</returns>
    public override async Task<long> CreateQueueSubscriptionAsync(TopicToQueueSubscription subscription, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subscription);

        long? result = await ExecuteScalarAsync<long>(_createQueueSubscriptionSql, new
        {
            SourceTopicName = subscription.Source.TopicName,
            DestinationQueueName = subscription.Destination.QueueName,
            SubscriptionType = (int)subscription.SubscriptionType,
            RoutingKey = subscription.RoutingKey ?? string.Empty,
            Filter = "{}"
        }, cancellationToken).ConfigureAwait(false);

        return result ?? throw new SqlTopologyException(
            $"SQL Server did not return an identifier for subscription '{subscription.Source.TopicName}' to '{subscription.Destination.QueueName}'.");
    }

    /// <summary>Removes currently available messages from a queue.</summary>
    /// <param name="queueName">The queue to purge.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The number of messages removed.</returns>
    public override async Task<long> PurgeQueueAsync(string queueName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);

        long? result = await ExecuteScalarAsync<long>(
            _purgeQueueSql,
            new { QueueName = queueName },
            cancellationToken).ConfigureAwait(false);

        return result ?? throw new SqlTopologyException($"SQL Server did not report a purge result for queue '{queueName}'.");
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
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(messageLimit);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(concurrentLimit);

        if (!Enum.IsDefined(mode))
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "The SQL receive mode is not defined.");
        if (lockDuration < TimeSpan.FromSeconds(1))
            throw new ArgumentOutOfRangeException(nameof(lockDuration), lockDuration, "The lock duration must be at least one second.");

        try
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
                }, cancellationToken).ConfigureAwait(false);
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
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (SqlException exception) when (exception.Number == 1205)
        {
            return [];
        }
    }

    /// <summary>Updates a queue's last-used timestamp.</summary>
    /// <param name="queueName">The queue to mark as used.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes after SQL Server updates the queue timestamp.</returns>
    public override async Task TouchQueueAsync(string queueName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);

        _ = await ExecuteScalarAsync<long>(_touchQueueSql, new { queueName }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Moves eligible messages from a queue to its dead-letter queue.</summary>
    /// <param name="queueName">The source queue.</param>
    /// <param name="messageCount">The maximum number of messages to move.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The number of messages moved, or <see langword="null" /> when the operation produces no result.</returns>
    public override Task<int?> DeadLetterQueueAsync(string queueName, int messageCount, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(messageCount);

        return ExecuteScalarAsync<int>(_deadLetterMessagesSql, new { queueName, messageCount }, cancellationToken);
    }

    /// <summary>Enqueues a message and its transport metadata in a queue.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="queueName">The destination queue.</param>
    /// <param name="context">The serialized message and send metadata.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes after SQL Server enqueues the message.</returns>
    public override async Task SendAsync<T>(string queueName, SqlMessageSendContext<T> context, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        ArgumentNullException.ThrowIfNull(context);

        _ = await ExecuteScalarAsync<long>(
            _sendSql,
            CreateMessageParameters(queueName, context),
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Publishes a message and its transport metadata through a topic.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="topicName">The source topic used to resolve subscriptions.</param>
    /// <param name="context">The serialized message and publish metadata.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes after SQL Server publishes the message to matching subscriptions.</returns>
    public override async Task PublishAsync<T>(string topicName, SqlMessageSendContext<T> context, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topicName);
        ArgumentNullException.ThrowIfNull(context);

        _ = await ExecuteScalarAsync<long>(
            _publishSql,
            CreateMessageParameters(topicName, context),
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Deletes a delivered message when the supplied lock still owns it.</summary>
    /// <param name="lockId">The current delivery lock identifier.</param>
    /// <param name="messageDeliveryId">The delivery identifier.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns><see langword="true" /> when the delivery was deleted; otherwise, <see langword="false" />.</returns>
    public override async Task<bool> DeleteMessageAsync(Guid lockId, long messageDeliveryId, CancellationToken cancellationToken = default)
    {
        ValidateLockIdentity(lockId, messageDeliveryId);

        long? result = await ExecuteScalarAsync<long>(_deleteMessageSql, new
        {
            messageDeliveryId,
            lockId
        }, cancellationToken).ConfigureAwait(false);

        return result == messageDeliveryId;
    }

    /// <summary>Deletes a scheduled message identified by its scheduling token.</summary>
    /// <param name="tokenId">The scheduling token identifier.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns><see langword="true" /> when a scheduled message was deleted; otherwise, <see langword="false" />.</returns>
    public override async Task<bool> DeleteScheduledMessageAsync(Guid tokenId, CancellationToken cancellationToken = default)
    {
        if (tokenId == Guid.Empty)
            throw new ArgumentException("The scheduling token must not be empty.", nameof(tokenId));

        IEnumerable<SqlTransportMessage> result = await QueryAsync<SqlTransportMessage>(_deleteScheduledMessageSql, new
        {
            tokenId
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
        ValidateLockIdentity(lockId, messageDeliveryId);
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        ArgumentNullException.ThrowIfNull(sendHeaders);

        if (!Enum.IsDefined(queueType))
            throw new ArgumentOutOfRangeException(nameof(queueType), queueType, "The SQL queue type is not defined.");

        long? result = await ExecuteScalarAsync<long>(_moveMessageTypeSql, new
        {
            messageDeliveryId,
            lockId,
            queueName,
            queueType = (int)queueType,
            expirationTime,
            headers = SerializeHeaders(sendHeaders)
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
        ValidateLockIdentity(lockId, messageDeliveryId);
        if (duration < TimeSpan.FromSeconds(1))
            throw new ArgumentOutOfRangeException(nameof(duration), duration, "The renewal duration must be at least one second.");

        long? result = await ExecuteScalarAsync<long>(_renewMessageLockSql, new
        {
            messageDeliveryId,
            lockId,
            duration = (int)duration.TotalSeconds
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
        ValidateLockIdentity(lockId, messageDeliveryId);
        ArgumentNullException.ThrowIfNull(sendHeaders);
        if (delay < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(delay), delay, "The unlock delay must not be negative.");

        long? result = await ExecuteScalarAsync<long>(_unlockSql, new
        {
            messageDeliveryId,
            lockId,
            delay = delay > TimeSpan.Zero ? Math.Max((int)delay.TotalSeconds, 1) : 0,
            headers = SerializeHeaders(sendHeaders)
        }, cancellationToken).ConfigureAwait(false);

        return result == messageDeliveryId;
    }

    Task<T?> ExecuteScalarAsync<T>(string procedureName, object values, CancellationToken cancellationToken)
        where T : struct
    {
        return ExecuteDatabaseOperationAsync((connection, transaction, token) =>
        {
            var command = new CommandDefinition(
                procedureName,
                values,
                transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: token);

            return connection.ExecuteScalarAsync<T?>(command);
        }, cancellationToken);
    }

    Task<IEnumerable<T>> QueryAsync<T>(string procedureName, object values, CancellationToken cancellationToken)
        where T : class
    {
        return ExecuteDatabaseOperationAsync((connection, transaction, token) =>
        {
            var command = new CommandDefinition(
                procedureName,
                values,
                transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: token);

            return connection.QueryAsync<T>(command);
        }, cancellationToken);
    }

    static object CreateMessageParameters<T>(string entityName, SqlMessageSendContext<T> context)
        where T : class
    {
        SqlMessageBodyStorage bodyStorage = SqlMessageBodyStorage.Create(context.Body, context.ContentType);
        Guid? schedulingTokenId = context.Headers.Get<Guid>(MessageHeaders.SchedulingTokenId);
        DateTime? expirationTime = context.TimeToLive.HasValue
            ? context.GetTimeProvider().GetUtcNow().UtcDateTime + context.TimeToLive.Value
            : null;

        return new
        {
            entityName,
            priority = (int)(context.Priority ?? 100),
            transportMessageId = context.TransportMessageId,
            body = bodyStorage.Text,
            binaryBody = bodyStorage.Binary,
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
            headers = SerializeHeaders(context.Headers),
            host = HostInfoCache.HostInfoJson,
            partitionKey = context.PartitionKey,
            routingKey = context.RoutingKey,
            delay = (int?)context.Delay?.TotalSeconds,
            schedulingTokenId
        };
    }

    static string? SerializeHeaders(Headers headers)
    {
        IEnumerable<KeyValuePair<string, object>> values = headers.GetAll().ToList();
        return values.Any() ? JsonSerializer.Serialize(values, ServiceBusMetadataJson.Options) : null;
    }

    static void ValidateLockIdentity(Guid lockId, long messageDeliveryId)
    {
        if (lockId == Guid.Empty)
            throw new ArgumentException("The delivery lock identifier must not be empty.", nameof(lockId));

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(messageDeliveryId);
    }
}
