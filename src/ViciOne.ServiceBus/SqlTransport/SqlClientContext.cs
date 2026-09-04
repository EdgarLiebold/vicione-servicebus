using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.SqlTransport.Topology;

namespace ViciOne.ServiceBus.SqlTransport;

public abstract class SqlClientContext :
    ScopePipeContext,
    ClientContext
{
    protected SqlClientContext(ConnectionContext context, CancellationToken cancellationToken)
        : base(context)
    {
        ConnectionContext = context;
        CancellationToken = cancellationToken;
    }

    public override CancellationToken CancellationToken { get; }

    public ConnectionContext ConnectionContext { get; }

    public abstract Task<long> CreateQueueAsync(Queue queue, CancellationToken cancellationToken = default);
    public abstract Task<long> CreateTopicAsync(Topic topic, CancellationToken cancellationToken = default);
    public abstract Task<long> CreateTopicSubscriptionAsync(TopicToTopicSubscription subscription, CancellationToken cancellationToken = default);
    public abstract Task<long> CreateQueueSubscriptionAsync(TopicToQueueSubscription subscription, CancellationToken cancellationToken = default);
    public abstract Task<long> PurgeQueueAsync(string queueName, CancellationToken cancellationToken);

    public abstract Task SendAsync<T>(string queueName, SqlMessageSendContext<T> context, CancellationToken cancellationToken = default)
        where T : class;

    public abstract Task PublishAsync<T>(string topicName, SqlMessageSendContext<T> context, CancellationToken cancellationToken = default)
        where T : class;

    public abstract Task<IEnumerable<SqlTransportMessage>> ReceiveMessagesAsync(string queueName, SqlReceiveMode mode, int messageLimit, int concurrentLimit,
        TimeSpan lockDuration, CancellationToken cancellationToken = default);

    public abstract Task TouchQueueAsync(string queueName, CancellationToken cancellationToken = default);
    public abstract Task<int?> DeadLetterQueueAsync(string queueName, int messageCount, CancellationToken cancellationToken = default);

    public abstract Task<bool> DeleteMessageAsync(Guid lockId, long messageDeliveryId, CancellationToken cancellationToken = default);
    public abstract Task<bool> DeleteScheduledMessageAsync(Guid tokenId, CancellationToken cancellationToken);
    public abstract Task<bool> MoveMessageAsync(Guid lockId, long messageDeliveryId, string queueName, SqlQueueType queueType, DateTimeOffset? expirationTime,
        SendHeaders sendHeaders, CancellationToken cancellationToken = default);
    public abstract Task<bool> RenewLockAsync(Guid lockId, long messageDeliveryId, TimeSpan duration, CancellationToken cancellationToken = default);
    public abstract Task<bool> UnlockAsync(Guid lockId, long messageDeliveryId, TimeSpan delay, SendHeaders sendHeaders, CancellationToken cancellationToken = default);
}
