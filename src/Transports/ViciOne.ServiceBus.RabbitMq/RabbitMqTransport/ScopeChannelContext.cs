using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Links scoped cancellation to an existing RabbitMQ channel context without owning the channel.</summary>
public class ScopeChannelContext :
    ScopePipeContext,
    ChannelContext,
    IDisposable
{
    readonly CancellationToken _cancellationToken;
    readonly ChannelContext _context;
    CancellationTokenSource? _tokenSource;

    /// <summary>Creates a scoped view whose cancellation combines parent and caller tokens.</summary>
    /// <param name="context">The shared RabbitMQ channel context.</param>
    /// <param name="cancellationToken">Cancellation for this scoped view.</param>
    public ScopeChannelContext(ChannelContext context, CancellationToken cancellationToken)
        : base(context)
    {
        _context = context;

        _cancellationToken = cancellationToken;
        _tokenSource = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken, cancellationToken);
    }

    /// <summary>Gets the token that combines parent-channel and scoped cancellation.</summary>
    public override CancellationToken CancellationToken => _tokenSource?.Token ?? _cancellationToken;

    /// <summary>Gets the underlying RabbitMQ channel.</summary>
    public IChannel Channel => _context.Channel;

    /// <summary>Gets the connection context that owns the channel.</summary>
    public ConnectionContext ConnectionContext => _context.ConnectionContext;

    /// <summary>Publishes through the active RabbitMQ channel.</summary>
    /// <param name="exchange">The destination exchange.</param>
    /// <param name="routingKey">The publish routing key.</param>
    /// <param name="mandatory">Whether RabbitMQ must return an unroutable message.</param>
    /// <param name="basicProperties">The AMQP message properties.</param>
    /// <param name="body">The serialized message body.</param>
    /// <param name="awaitAck">Whether the caller awaits the client publish outcome.</param>
    /// <param name="cancellationToken">Additional cancellation for this operation.</param>
    /// <returns>A task that follows the underlying publish according to <paramref name="awaitAck" />.</returns>
    public async Task BasicPublishAsync(string exchange, string routingKey, bool mandatory, BasicProperties basicProperties, byte[] body, bool awaitAck,
        CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.BasicPublishAsync(exchange, routingKey, mandatory, basicProperties, body, awaitAck, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>Creates an exchange-to-exchange binding through the shared channel.</summary>
    /// <param name="destination">The destination exchange.</param>
    /// <param name="source">The source exchange.</param>
    /// <param name="routingKey">The binding routing key.</param>
    /// <param name="arguments">The RabbitMQ binding arguments.</param>
    /// <param name="cancellationToken">Additional cancellation for this operation.</param>
    /// <returns>A task that completes when RabbitMQ accepts the binding.</returns>
    public async Task ExchangeBindAsync(string destination, string source, string routingKey, IDictionary<string, object?> arguments,
        CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.ExchangeBindAsync(destination, source, routingKey, arguments, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>Declares an exchange through the shared channel.</summary>
    /// <param name="exchange">The exchange name.</param>
    /// <param name="type">The RabbitMQ exchange type.</param>
    /// <param name="durable">Whether the exchange survives broker restarts.</param>
    /// <param name="autoDelete">Whether RabbitMQ deletes the exchange when unused.</param>
    /// <param name="arguments">The exchange declaration arguments.</param>
    /// <param name="cancellationToken">Additional cancellation for this operation.</param>
    /// <returns>A task that completes when RabbitMQ accepts the declaration.</returns>
    public async Task ExchangeDeclareAsync(string exchange, string type, bool durable, bool autoDelete, IDictionary<string, object?> arguments,
        CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.ExchangeDeclareAsync(exchange, type, durable, autoDelete, arguments, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>Passively verifies an exchange through the shared channel.</summary>
    /// <param name="exchange">The exchange name.</param>
    /// <param name="cancellationToken">Additional cancellation for this operation.</param>
    /// <returns>A task that completes when RabbitMQ confirms the exchange exists.</returns>
    public async Task ExchangeDeclarePassiveAsync(string exchange, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.ExchangeDeclarePassiveAsync(exchange, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>Creates an exchange-to-queue binding through the shared channel.</summary>
    /// <param name="queue">The destination queue.</param>
    /// <param name="exchange">The source exchange.</param>
    /// <param name="routingKey">The binding routing key.</param>
    /// <param name="arguments">The RabbitMQ binding arguments.</param>
    /// <param name="cancellationToken">Additional cancellation for this operation.</param>
    /// <returns>A task that completes when RabbitMQ accepts the binding.</returns>
    public async Task QueueBindAsync(string queue, string exchange, string routingKey, IDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.QueueBindAsync(queue, exchange, routingKey, arguments, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>Declares a queue through the shared channel.</summary>
    /// <param name="queue">The queue name, or an empty string for a broker-generated name.</param>
    /// <param name="durable">Whether the queue survives broker restarts.</param>
    /// <param name="exclusive">Whether the queue belongs exclusively to this connection.</param>
    /// <param name="autoDelete">Whether RabbitMQ deletes the queue when unused.</param>
    /// <param name="arguments">The queue declaration arguments.</param>
    /// <param name="cancellationToken">Additional cancellation for this operation.</param>
    /// <returns>The broker's queue declaration result.</returns>
    public async Task<QueueDeclareOk> QueueDeclareAsync(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object?> arguments,
        CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.QueueDeclareAsync(queue, durable, exclusive, autoDelete, arguments, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>Passively verifies a queue through the shared channel.</summary>
    /// <param name="queue">The queue name.</param>
    /// <param name="cancellationToken">Additional cancellation for this operation.</param>
    /// <returns>The broker's passive queue declaration result.</returns>
    public async Task<QueueDeclareOk> QueueDeclarePassiveAsync(string queue, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.QueueDeclarePassiveAsync(queue, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>Removes every pending message from the queue.</summary>
    /// <param name="queue">The queue name.</param>
    /// <param name="cancellationToken">Additional cancellation for this operation.</param>
    /// <returns>The number of messages removed.</returns>
    public async Task<uint> QueuePurgeAsync(string queue, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.QueuePurgeAsync(queue, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>Applies the configured RabbitMQ quality-of-service limits.</summary>
    /// <param name="prefetchSize">The AMQP prefetch-size limit.</param>
    /// <param name="prefetchCount">The maximum number of unacknowledged deliveries.</param>
    /// <param name="global">Whether the limit applies to every consumer on the channel.</param>
    /// <param name="cancellationToken">Additional cancellation for this operation.</param>
    /// <returns>A task that completes when RabbitMQ applies the limit.</returns>
    public async Task BasicQosAsync(uint prefetchSize, ushort prefetchCount, bool global, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.BasicQosAsync(prefetchSize, prefetchCount, global, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>Acknowledges the selected RabbitMQ delivery.</summary>
    /// <param name="deliveryTag">The channel-scoped delivery tag.</param>
    /// <param name="multiple">Whether to acknowledge this tag and every preceding unacknowledged delivery.</param>
    /// <param name="cancellationToken">Additional cancellation for this operation.</param>
    /// <returns>A task-like value that completes after the acknowledgement is written.</returns>
    public async ValueTask BasicAckAsync(ulong deliveryTag, bool multiple, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.BasicAckAsync(deliveryTag, multiple, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>Rejects the selected RabbitMQ delivery.</summary>
    /// <param name="deliveryTag">The channel-scoped delivery tag.</param>
    /// <param name="multiple">Whether to reject this tag and every preceding unacknowledged delivery.</param>
    /// <param name="requeue">Whether RabbitMQ should place rejected deliveries back on their queues.</param>
    /// <param name="cancellationToken">Additional cancellation for this operation.</param>
    /// <returns>A task that completes after the negative acknowledgement is written.</returns>
    public async Task BasicNackAsync(ulong deliveryTag, bool multiple, bool requeue, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.BasicNackAsync(deliveryTag, multiple, requeue, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>Starts the configured RabbitMQ consumer.</summary>
    /// <param name="queue">The source queue name.</param>
    /// <param name="noAck">Whether RabbitMQ should consider deliveries acknowledged immediately.</param>
    /// <param name="exclusive">Whether the broker permits only this consumer on the queue.</param>
    /// <param name="arguments">The consumer arguments.</param>
    /// <param name="consumer">The callback receiver for deliveries and lifecycle events.</param>
    /// <param name="consumerTag">The requested consumer tag, or an empty string for a generated tag.</param>
    /// <param name="cancellationToken">Additional cancellation for this operation.</param>
    /// <returns>The consumer tag assigned by RabbitMQ.</returns>
    public async Task<string> BasicConsumeAsync(string queue, bool noAck, bool exclusive, IDictionary<string, object?> arguments, IAsyncBasicConsumer consumer,
        string consumerTag, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.BasicConsumeAsync(queue, noAck, exclusive, arguments, consumer, consumerTag, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>Cancels the active RabbitMQ consumer.</summary>
    /// <param name="consumerTag">The tag of the consumer to cancel.</param>
    /// <param name="cancellationToken">Additional cancellation for this operation.</param>
    /// <returns>A task that completes when the cancel command has been sent.</returns>
    public async Task BasicCancelAsync(string consumerTag, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.BasicCancelAsync(consumerTag, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>Forwards an unrecoverable channel fault to the owning context.</summary>
    /// <param name="exception">The channel or consumer failure.</param>
    /// <param name="contextInputAddress">The receive endpoint address affected by the fault.</param>
    public void NotifyFaulted(Exception exception, Uri contextInputAddress)
    {
        _context.NotifyFaulted(exception, contextInputAddress);
    }

    /// <summary>Releases the resources owned by this instance.</summary>
    public void Dispose()
    {
        _tokenSource?.Dispose();
        _tokenSource = null;
    }
}
