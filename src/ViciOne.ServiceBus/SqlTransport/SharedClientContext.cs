using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.SqlTransport.Topology;

namespace ViciOne.ServiceBus.SqlTransport;

public class SharedClientContext :
    ProxyPipeContext,
    ClientContext
{
    readonly ClientContext _context;

    public SharedClientContext(ClientContext context, CancellationToken cancellationToken)
        : base(context)
    {
        _context = context;
        CancellationToken = cancellationToken;
    }

    public override CancellationToken CancellationToken { get; }

    public ConnectionContext ConnectionContext => _context.ConnectionContext;

    public Task<long> CreateQueueAsync(Queue queue, CancellationToken cancellationToken = default)
    {
        return _context.CreateQueueAsync(queue, cancellationToken: cancellationToken);
    }

    public Task<long> CreateTopicAsync(Topic topic, CancellationToken cancellationToken = default)
    {
        return _context.CreateTopicAsync(topic, cancellationToken: cancellationToken);
    }

    public Task<long> CreateTopicSubscriptionAsync(TopicToTopicSubscription subscription, CancellationToken cancellationToken = default)
    {
        return _context.CreateTopicSubscriptionAsync(subscription, cancellationToken: cancellationToken);
    }

    public Task<long> CreateQueueSubscriptionAsync(TopicToQueueSubscription subscription, CancellationToken cancellationToken = default)
    {
        return _context.CreateQueueSubscriptionAsync(subscription, cancellationToken: cancellationToken);
    }

    public Task<long> PurgeQueueAsync(string queueName, CancellationToken cancellationToken)
    {
        return _context.PurgeQueueAsync(queueName, cancellationToken);
    }

    public Task SendAsync<T>(string queueName, SqlMessageSendContext<T> context, CancellationToken cancellationToken = default)
        where T : class
    {
        return _context.SendAsync(queueName, context, cancellationToken: cancellationToken);
    }

    public Task PublishAsync<T>(string topicName, SqlMessageSendContext<T> context, CancellationToken cancellationToken = default)
        where T : class
    {
        return _context.PublishAsync(topicName, context, cancellationToken: cancellationToken);
    }

    public Task<bool> RenewLockAsync(Guid lockId, long messageDeliveryId, TimeSpan duration, CancellationToken cancellationToken = default)
    {
        return _context.RenewLockAsync(lockId, messageDeliveryId, duration, cancellationToken: cancellationToken);
    }

    public Task<bool> UnlockAsync(Guid lockId, long messageDeliveryId, TimeSpan delay, SendHeaders sendHeaders, CancellationToken cancellationToken = default)
    {
        return _context.UnlockAsync(lockId, messageDeliveryId, delay, sendHeaders, cancellationToken: cancellationToken);
    }

    public Task<IEnumerable<SqlTransportMessage>> ReceiveMessagesAsync(string queueName, SqlReceiveMode mode, int messageLimit, int concurrentLimit,
        TimeSpan lockDuration, CancellationToken cancellationToken = default)
    {
        return _context.ReceiveMessagesAsync(queueName, mode, messageLimit, concurrentLimit, lockDuration, cancellationToken: cancellationToken);
    }

    public Task TouchQueueAsync(string queueName, CancellationToken cancellationToken = default)
    {
        return _context.TouchQueueAsync(queueName, cancellationToken: cancellationToken);
    }

    public Task<int?> DeadLetterQueueAsync(string queueName, int messageCount, CancellationToken cancellationToken = default)
    {
        return _context.DeadLetterQueueAsync(queueName, messageCount, cancellationToken: cancellationToken);
    }

    public Task<bool> DeleteMessageAsync(Guid lockId, long messageDeliveryId, CancellationToken cancellationToken = default)
    {
        return _context.DeleteMessageAsync(lockId, messageDeliveryId, cancellationToken: cancellationToken);
    }

    public Task<bool> DeleteScheduledMessageAsync(Guid tokenId, CancellationToken cancellationToken)
    {
        return _context.DeleteScheduledMessageAsync(tokenId, cancellationToken);
    }

    public Task<bool> MoveMessageAsync(Guid lockId, long messageDeliveryId, string queueName, SqlQueueType queueType, DateTimeOffset? expirationTime,
        SendHeaders sendHeaders, CancellationToken cancellationToken = default)
    {
        return _context.MoveMessageAsync(lockId, messageDeliveryId, queueName, queueType, expirationTime, sendHeaders, cancellationToken: cancellationToken);
    }
}
