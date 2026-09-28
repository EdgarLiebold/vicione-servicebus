using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Delegates AMQP operations to a shared channel while combining the channel lease and caller cancellation tokens.</summary>
public class SharedChannelContext :
    ProxyPipeContext,
    ChannelContext
{
    readonly ChannelContext _context;

    /// <summary>Creates a cancellable view over an existing channel context.</summary>
    /// <param name="context">The underlying channel context.</param>
    /// <param name="cancellationToken">The token that ends this shared-channel lease.</param>
    public SharedChannelContext(ChannelContext context, CancellationToken cancellationToken)
        : base(context)
    {
        _context = context;
        CancellationToken = cancellationToken;
    }

    /// <summary>Gets the token that ends this shared-channel lease.</summary>
    public override CancellationToken CancellationToken { get; }

    /// <summary>Gets the underlying RabbitMQ channel.</summary>
    public IChannel Channel => _context.Channel;

    /// <summary>Gets the connection context that owns the channel.</summary>
    public ConnectionContext ConnectionContext => _context.ConnectionContext;

    /// <summary>Publishes a message through the underlying channel.</summary>
    /// <param name="exchange">The destination exchange name.</param>
    /// <param name="routingKey">The routing key supplied to the exchange.</param>
    /// <param name="mandatory">Whether an unroutable message must be returned by the broker.</param>
    /// <param name="basicProperties">The AMQP message properties.</param>
    /// <param name="body">The serialized message body.</param>
    /// <param name="awaitAck">Whether to await the client publish task, including publisher confirmation when enabled.</param>
    /// <param name="cancellationToken">The token that cancels this publish in addition to the shared-channel token.</param>
    /// <returns>A task that completes with the delegated publish when <paramref name="awaitAck"/> is true, or immediately after starting it otherwise.</returns>
    public Task BasicPublishAsync(string exchange, string routingKey, bool mandatory, BasicProperties basicProperties, byte[] body, bool awaitAck,
        CancellationToken cancellationToken)
    {
        var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);
        Task publish;
        try
        {
            // Observe the actual SDK operation so its cancellation link remains active even for no-ack callers.
            publish = _context.BasicPublishAsync(exchange, routingKey, mandatory, basicProperties, body, true, tokenSource.Token);
        }
        catch
        {
            tokenSource.Dispose();
            throw;
        }

        async Task CompleteAndReleaseAsync()
        {
            try
            {
                await publish.ConfigureAwait(false);
            }
            finally
            {
                tokenSource.Dispose();
            }
        }

        Task tracked = CompleteAndReleaseAsync();
        if (awaitAck)
            return tracked;

        tracked.IgnoreUnobservedExceptions();
        return Task.CompletedTask;
    }

    /// <summary>Binds a source exchange to a destination exchange.</summary>
    /// <param name="destination">The exchange that receives messages from the binding.</param>
    /// <param name="source">The exchange whose messages are routed by the binding.</param>
    /// <param name="routingKey">The routing key used by the binding.</param>
    /// <param name="arguments">The broker-specific binding arguments.</param>
    /// <param name="cancellationToken">The token that cancels this declaration in addition to the shared-channel token.</param>
    /// <returns>A task that completes when RabbitMQ has created the binding.</returns>
    public async Task ExchangeBindAsync(string destination, string source, string routingKey, IDictionary<string, object?> arguments,
        CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.ExchangeBindAsync(destination, source, routingKey, arguments, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>Declares an exchange with the supplied AMQP properties.</summary>
    /// <param name="exchange">The exchange name.</param>
    /// <param name="type">The RabbitMQ exchange type.</param>
    /// <param name="durable">Whether the exchange survives broker restarts.</param>
    /// <param name="autoDelete">Whether RabbitMQ deletes the exchange when it is no longer used.</param>
    /// <param name="arguments">The broker-specific declaration arguments.</param>
    /// <param name="cancellationToken">The token that cancels this declaration in addition to the shared-channel token.</param>
    /// <returns>A task that completes when RabbitMQ has declared the exchange.</returns>
    public async Task ExchangeDeclareAsync(string exchange, string type, bool durable, bool autoDelete, IDictionary<string, object?> arguments,
        CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.ExchangeDeclareAsync(exchange, type, durable, autoDelete, arguments, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>Performs a passive declaration to verify that an exchange exists.</summary>
    /// <param name="exchange">The exchange name.</param>
    /// <param name="cancellationToken">The token that cancels this check in addition to the shared-channel token.</param>
    /// <returns>A task that completes when RabbitMQ confirms that the exchange exists.</returns>
    public async Task ExchangeDeclarePassiveAsync(string exchange, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.ExchangeDeclarePassiveAsync(exchange, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>Binds a queue to an exchange.</summary>
    /// <param name="queue">The destination queue name.</param>
    /// <param name="exchange">The source exchange name.</param>
    /// <param name="routingKey">The routing key used by the binding.</param>
    /// <param name="arguments">The broker-specific binding arguments.</param>
    /// <param name="cancellationToken">The token that cancels this declaration in addition to the shared-channel token.</param>
    /// <returns>A task that completes when RabbitMQ has created the binding.</returns>
    public async Task QueueBindAsync(string queue, string exchange, string routingKey, IDictionary<string, object?> arguments,
        CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.QueueBindAsync(queue, exchange, routingKey, arguments, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>Declares a queue with the supplied AMQP properties.</summary>
    /// <param name="queue">The queue name, or an empty string to request a broker-generated name.</param>
    /// <param name="durable">Whether the queue survives broker restarts.</param>
    /// <param name="exclusive">Whether the queue belongs exclusively to this connection.</param>
    /// <param name="autoDelete">Whether RabbitMQ deletes the queue when its last consumer is gone.</param>
    /// <param name="arguments">The broker-specific declaration arguments.</param>
    /// <param name="cancellationToken">The token that cancels this declaration in addition to the shared-channel token.</param>
    /// <returns>The broker's queue-declaration result.</returns>
    public async Task<QueueDeclareOk> QueueDeclareAsync(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object?> arguments,
        CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.QueueDeclareAsync(queue, durable, exclusive, autoDelete, arguments, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>Performs a passive declaration to verify that a queue exists.</summary>
    /// <param name="queue">The queue name.</param>
    /// <param name="cancellationToken">The token that cancels this check in addition to the shared-channel token.</param>
    /// <returns>The broker's passive queue-declaration result.</returns>
    public async Task<QueueDeclareOk> QueueDeclarePassiveAsync(string queue, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.QueueDeclarePassiveAsync(queue, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>Removes every ready message from a queue.</summary>
    /// <param name="queue">The queue name.</param>
    /// <param name="cancellationToken">The token that cancels this purge in addition to the shared-channel token.</param>
    /// <returns>The number of messages removed by RabbitMQ.</returns>
    public async Task<uint> QueuePurgeAsync(string queue, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.QueuePurgeAsync(queue, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>Applies RabbitMQ quality-of-service limits to the channel.</summary>
    /// <param name="prefetchSize">The prefetch size limit in octets; RabbitMQ currently ignores this value.</param>
    /// <param name="prefetchCount">The maximum number of unacknowledged deliveries.</param>
    /// <param name="global">Whether the limit applies to the channel rather than to each consumer.</param>
    /// <param name="cancellationToken">The token that cancels this operation in addition to the shared-channel token.</param>
    /// <returns>A task that completes when RabbitMQ has applied the limits.</returns>
    public async Task BasicQosAsync(uint prefetchSize, ushort prefetchCount, bool global, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.BasicQosAsync(prefetchSize, prefetchCount, global, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>Acknowledges one or more deliveries on the channel.</summary>
    /// <param name="deliveryTag">The delivery tag that identifies the delivery.</param>
    /// <param name="multiple">Whether to acknowledge all unacknowledged deliveries through <paramref name="deliveryTag"/>.</param>
    /// <param name="cancellationToken">The token that cancels this acknowledgement in addition to the shared-channel token.</param>
    /// <returns>A value task that completes after the acknowledgement has been written to the channel.</returns>
    public async ValueTask BasicAckAsync(ulong deliveryTag, bool multiple, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.BasicAckAsync(deliveryTag, multiple, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>Negatively acknowledges one or more deliveries on the channel.</summary>
    /// <param name="deliveryTag">The delivery tag that identifies the delivery.</param>
    /// <param name="multiple">Whether to reject all unacknowledged deliveries through <paramref name="deliveryTag"/>.</param>
    /// <param name="requeue">Whether RabbitMQ should make the rejected deliveries available again.</param>
    /// <param name="cancellationToken">The token that cancels this rejection in addition to the shared-channel token.</param>
    /// <returns>A task that completes after the rejection has been written to the channel.</returns>
    public async Task BasicNackAsync(ulong deliveryTag, bool multiple, bool requeue, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.BasicNackAsync(deliveryTag, multiple, requeue, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>Registers an asynchronous consumer for a queue.</summary>
    /// <param name="queue">The queue name.</param>
    /// <param name="noAck">Whether RabbitMQ should consider deliveries acknowledged immediately.</param>
    /// <param name="exclusive">Whether this consumer has exclusive access to the queue.</param>
    /// <param name="arguments">The broker-specific consumer arguments.</param>
    /// <param name="consumer">The callback object that handles broker deliveries and lifecycle notifications.</param>
    /// <param name="consumerTag">The requested consumer tag, or an empty string for a broker-generated tag.</param>
    /// <param name="cancellationToken">The token that cancels this registration in addition to the shared-channel token.</param>
    /// <returns>The consumer tag assigned by RabbitMQ.</returns>
    public async Task<string> BasicConsumeAsync(string queue, bool noAck, bool exclusive, IDictionary<string, object?> arguments, IAsyncBasicConsumer consumer,
        string consumerTag, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.BasicConsumeAsync(queue, noAck, exclusive, arguments, consumer, consumerTag, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>Cancels a consumer registration.</summary>
    /// <param name="consumerTag">The tag assigned to the consumer.</param>
    /// <param name="cancellationToken">The token that cancels this request in addition to the shared-channel token.</param>
    /// <returns>A task that completes when RabbitMQ confirms the cancellation.</returns>
    public async Task BasicCancelAsync(string consumerTag, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.BasicCancelAsync(consumerTag, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>Forwards a receive-pipeline fault to the underlying channel context.</summary>
    /// <param name="exception">The exception raised by the receive pipeline.</param>
    /// <param name="contextInputAddress">The input address associated with the fault.</param>
    public void NotifyFaulted(Exception exception, Uri contextInputAddress)
    {
        _context.NotifyFaulted(exception, contextInputAddress);
    }
}
