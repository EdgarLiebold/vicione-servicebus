using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.RabbitMqTransport;

public class SharedChannelContext :
    ProxyPipeContext,
    ChannelContext
{
    readonly ChannelContext _context;

    public SharedChannelContext(ChannelContext context, CancellationToken cancellationToken)
        : base(context)
    {
        _context = context;
        CancellationToken = cancellationToken;
    }

    public override CancellationToken CancellationToken { get; }

    public IChannel Channel => _context.Channel;

    public ConnectionContext ConnectionContext => _context.ConnectionContext;

    public async Task BasicPublishAsync(string exchange, string routingKey, bool mandatory, BasicProperties basicProperties, byte[] body, bool awaitAck,
        CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.BasicPublishAsync(exchange, routingKey, mandatory, basicProperties, body, awaitAck, tokenSource.Token).ConfigureAwait(false);
    }

    public async Task ExchangeBindAsync(string destination, string source, string routingKey, IDictionary<string, object?> arguments,
        CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.ExchangeBindAsync(destination, source, routingKey, arguments, tokenSource.Token).ConfigureAwait(false);
    }

    public async Task ExchangeDeclareAsync(string exchange, string type, bool durable, bool autoDelete, IDictionary<string, object?> arguments,
        CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.ExchangeDeclareAsync(exchange, type, durable, autoDelete, arguments, tokenSource.Token).ConfigureAwait(false);
    }

    public async Task ExchangeDeclarePassiveAsync(string exchange, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.ExchangeDeclarePassiveAsync(exchange, tokenSource.Token).ConfigureAwait(false);
    }

    public async Task QueueBindAsync(string queue, string exchange, string routingKey, IDictionary<string, object?> arguments,
        CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.QueueBindAsync(queue, exchange, routingKey, arguments, tokenSource.Token).ConfigureAwait(false);
    }

    public async Task<QueueDeclareOk> QueueDeclareAsync(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object?> arguments,
        CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.QueueDeclareAsync(queue, durable, exclusive, autoDelete, arguments, tokenSource.Token).ConfigureAwait(false);
    }

    public async Task<QueueDeclareOk> QueueDeclarePassiveAsync(string queue, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.QueueDeclarePassiveAsync(queue, tokenSource.Token).ConfigureAwait(false);
    }

    public async Task<uint> QueuePurgeAsync(string queue, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.QueuePurgeAsync(queue, tokenSource.Token).ConfigureAwait(false);
    }

    public async Task BasicQosAsync(uint prefetchSize, ushort prefetchCount, bool global, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.BasicQosAsync(prefetchSize, prefetchCount, global, tokenSource.Token).ConfigureAwait(false);
    }

    public async ValueTask BasicAckAsync(ulong deliveryTag, bool multiple, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.BasicAckAsync(deliveryTag, multiple, tokenSource.Token).ConfigureAwait(false);
    }

    public async Task BasicNackAsync(ulong deliveryTag, bool multiple, bool requeue, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.BasicNackAsync(deliveryTag, multiple, requeue, tokenSource.Token).ConfigureAwait(false);
    }

    public async Task<string> BasicConsumeAsync(string queue, bool noAck, bool exclusive, IDictionary<string, object?> arguments, IAsyncBasicConsumer consumer,
        string consumerTag, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.BasicConsumeAsync(queue, noAck, exclusive, arguments, consumer, consumerTag, tokenSource.Token).ConfigureAwait(false);
    }

    public async Task BasicCancelAsync(string consumerTag, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.BasicCancelAsync(consumerTag, tokenSource.Token).ConfigureAwait(false);
    }

    public void NotifyFaulted(Exception exception, Uri contextInputAddress)
    {
        _context.NotifyFaulted(exception, contextInputAddress);
    }
}
