using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;
using RabbitMqPublishException = RabbitMQ.Client.Exceptions.PublishException;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Owns one RabbitMQ channel and leases it to transport operations until disposal.</summary>
public class RabbitMqChannelContext :
    ScopePipeContext,
    ChannelContext,
    IAsyncDisposable
{
    readonly IAgent _agent;
    readonly CancellationToken _cancellationToken;
    readonly IChannel _channel;
    readonly object _faultStopLock = new object();

    /// <summary>
    /// Owns the channel. Every operation below runs under a lease so disposal cannot begin until
    /// the last in-flight channel operation has finished unwinding.
    /// </summary>
    readonly TransportLifetime _lifetime;

    Task? _faultStopTask;
    CancellationTokenSource? _tokenSource;

    /// <summary>Creates a lifetime-managed channel context linked to connection and caller cancellation.</summary>
    /// <param name="connectionContext">The owning RabbitMQ connection context.</param>
    /// <param name="channel">The RabbitMQ client channel.</param>
    /// <param name="agent">The transport agent stopped after an unrecoverable channel fault.</param>
    /// <param name="cancellationToken">Cancellation linked to the channel context.</param>
    public RabbitMqChannelContext(ConnectionContext connectionContext, IChannel channel, IAgent agent, CancellationToken cancellationToken)
        : base(connectionContext)
    {
        ConnectionContext = connectionContext;

        _channel = channel;
        _lifetime = new TransportLifetime("channel", () => channel.CleanupAsync(200, "ChannelContext Disposed"));
        _agent = agent;

        _cancellationToken = cancellationToken;
        _tokenSource = CancellationTokenSource.CreateLinkedTokenSource(connectionContext.CancellationToken, cancellationToken);
    }

    /// <summary>Gets the token that combines connection and channel-agent cancellation.</summary>
    public override CancellationToken CancellationToken => _tokenSource?.Token ?? _cancellationToken;

    /// <summary>Gets the owned RabbitMQ channel.</summary>
    public IChannel Channel => _channel;

    internal TransportLifetime Lifetime => _lifetime;

    /// <summary>Gets the connection context that created the channel.</summary>
    public ConnectionContext ConnectionContext { get; }

    /// <summary>Publishes through the active RabbitMQ channel.</summary>
    /// <param name="exchange">The destination exchange.</param>
    /// <param name="routingKey">The publish routing key.</param>
    /// <param name="mandatory">Whether RabbitMQ must return an unroutable message.</param>
    /// <param name="basicProperties">The AMQP message properties.</param>
    /// <param name="body">The serialized message body.</param>
    /// <param name="awaitAck"><see langword="true" /> to await the RabbitMQ client publish task, including publisher confirmation when enabled; otherwise, to return after initiating the client publish.</param>
    /// <param name="cancellationToken">Cancellation for the client publish.</param>
    /// <returns>
    /// A task that follows the RabbitMQ client publish operation when <paramref name="awaitAck" /> is <see langword="true" />;
    /// otherwise, a completed task while the channel lease continues to observe the client publish internally.
    /// </returns>
    public Task BasicPublishAsync(string exchange, string routingKey, bool mandatory, BasicProperties basicProperties, byte[] body, bool awaitAck,
        CancellationToken cancellationToken)
    {
        // The lease follows the client publish task even when the caller opts out of publisher-confirm waiting.
        // Both paths observe completion so disposal cannot overtake the in-flight channel operation.
        var lease = Lease();

        Task publish;
        try
        {
            publish = _channel.BasicPublishAsync(exchange, routingKey, mandatory, basicProperties, new ReadOnlyMemory<byte>(body),
                cancellationToken).AsTask();
        }
        catch
        {
            lease.Dispose();
            throw;
        }

        async Task PublishAndReleaseAsync()
        {
            try
            {
                await publish.ConfigureAwait(false);
            }
            catch (RabbitMqPublishException exception) when (exception.IsReturn)
            {
                throw new MessageReturnedException("The message was returned by RabbitMQ", exception);
            }
            catch (RabbitMqPublishException exception)
            {
                throw new RabbitMqConnectionException("BasicPublishAsync failed", exception);
            }
            finally
            {
                lease.Dispose();
            }
        }

        var published = PublishAndReleaseAsync();

        if (awaitAck)
            return published;

        published.IgnoreUnobservedExceptions();

        return Task.CompletedTask;
    }


    /// <summary>Creates an exchange-to-exchange binding under a channel lease.</summary>
    /// <param name="destination">The destination exchange.</param>
    /// <param name="source">The source exchange.</param>
    /// <param name="routingKey">The binding routing key.</param>
    /// <param name="arguments">The RabbitMQ binding arguments.</param>
    /// <param name="cancellationToken">Cancellation for the broker command.</param>
    /// <returns>A task that completes when RabbitMQ accepts the binding.</returns>
    public async Task ExchangeBindAsync(string destination, string source, string routingKey, IDictionary<string, object?> arguments,
        CancellationToken cancellationToken)
    {
        using var lease = Lease();

        await _channel.ExchangeBindAsync(destination, source, routingKey, arguments, false, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Declares an exchange under a channel lease.</summary>
    /// <param name="exchange">The exchange name.</param>
    /// <param name="type">The RabbitMQ exchange type.</param>
    /// <param name="durable">Whether the exchange survives broker restarts.</param>
    /// <param name="autoDelete">Whether RabbitMQ deletes the exchange when unused.</param>
    /// <param name="arguments">The exchange declaration arguments.</param>
    /// <param name="cancellationToken">Cancellation for the broker command.</param>
    /// <returns>A task that completes when RabbitMQ accepts the declaration.</returns>
    public async Task ExchangeDeclareAsync(string exchange, string type, bool durable, bool autoDelete, IDictionary<string, object?> arguments,
        CancellationToken cancellationToken)
    {
        using var lease = Lease();

        await _channel.ExchangeDeclareAsync(exchange, type, durable, autoDelete, arguments, false, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Passively verifies an exchange under a channel lease.</summary>
    /// <param name="exchange">The exchange name.</param>
    /// <param name="cancellationToken">Cancellation for the broker command.</param>
    /// <returns>A task that completes when RabbitMQ confirms the exchange exists.</returns>
    public async Task ExchangeDeclarePassiveAsync(string exchange, CancellationToken cancellationToken)
    {
        using var lease = Lease();

        await _channel.ExchangeDeclarePassiveAsync(exchange, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Creates an exchange-to-queue binding under a channel lease.</summary>
    /// <param name="queue">The destination queue.</param>
    /// <param name="exchange">The source exchange.</param>
    /// <param name="routingKey">The binding routing key.</param>
    /// <param name="arguments">The RabbitMQ binding arguments.</param>
    /// <param name="cancellationToken">Cancellation for the broker command.</param>
    /// <returns>A task that completes when RabbitMQ accepts the binding.</returns>
    public async Task QueueBindAsync(string queue, string exchange, string routingKey, IDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        using var lease = Lease();

        await _channel.QueueBindAsync(queue, exchange, routingKey, arguments, false, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Declares a queue under a channel lease.</summary>
    /// <param name="queue">The queue name, or an empty string for a broker-generated name.</param>
    /// <param name="durable">Whether the queue survives broker restarts.</param>
    /// <param name="exclusive">Whether the queue belongs exclusively to this connection.</param>
    /// <param name="autoDelete">Whether RabbitMQ deletes the queue when unused.</param>
    /// <param name="arguments">The queue declaration arguments.</param>
    /// <param name="cancellationToken">Cancellation for the broker command.</param>
    /// <returns>The broker's queue declaration result.</returns>
    public async Task<QueueDeclareOk> QueueDeclareAsync(string queue, bool durable, bool exclusive, bool autoDelete,
        IDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        using var lease = Lease();

        return await _channel.QueueDeclareAsync(queue, durable, exclusive, autoDelete, arguments, false, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Passively verifies a queue under a channel lease.</summary>
    /// <param name="queue">The queue name.</param>
    /// <param name="cancellationToken">Cancellation for the broker command.</param>
    /// <returns>The broker's passive queue declaration result.</returns>
    public async Task<QueueDeclareOk> QueueDeclarePassiveAsync(string queue, CancellationToken cancellationToken)
    {
        using var lease = Lease();

        return await _channel.QueueDeclarePassiveAsync(queue, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Removes every pending message from the queue.</summary>
    /// <param name="queue">The queue name.</param>
    /// <param name="cancellationToken">Cancellation for the broker command.</param>
    /// <returns>The number of messages removed.</returns>
    public async Task<uint> QueuePurgeAsync(string queue, CancellationToken cancellationToken)
    {
        using var lease = Lease();

        return await _channel.QueuePurgeAsync(queue, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Applies the configured RabbitMQ quality-of-service limits.</summary>
    /// <param name="prefetchSize">The AMQP prefetch-size limit.</param>
    /// <param name="prefetchCount">The maximum number of unacknowledged deliveries.</param>
    /// <param name="global">Whether the limit applies to every consumer on the channel.</param>
    /// <param name="cancellationToken">Cancellation for the broker command.</param>
    /// <returns>A task that completes when RabbitMQ applies the limit.</returns>
    public async Task BasicQosAsync(uint prefetchSize, ushort prefetchCount, bool global, CancellationToken cancellationToken)
    {
        using var lease = Lease();

        await _channel.BasicQosAsync(prefetchSize, prefetchCount, global, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Acknowledges the selected RabbitMQ delivery.</summary>
    /// <param name="deliveryTag">The channel-scoped delivery tag.</param>
    /// <param name="multiple">Whether to acknowledge this tag and every preceding unacknowledged delivery.</param>
    /// <param name="cancellationToken">Cancellation for writing the acknowledgement.</param>
    /// <returns>A task-like value that completes after the acknowledgement is written.</returns>
    public async ValueTask BasicAckAsync(ulong deliveryTag, bool multiple, CancellationToken cancellationToken)
    {
        // The leased call propagates the authoritative broker or transport acknowledgement failure.
        using var lease = Lease();

        await _channel.BasicAckAsync(deliveryTag, multiple, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Rejects the selected RabbitMQ delivery.</summary>
    /// <param name="deliveryTag">The channel-scoped delivery tag.</param>
    /// <param name="multiple">Whether to reject this tag and every preceding unacknowledged delivery.</param>
    /// <param name="requeue">Whether RabbitMQ should place rejected deliveries back on their queues.</param>
    /// <param name="cancellationToken">Cancellation for writing the rejection.</param>
    /// <returns>A task that completes after the negative acknowledgement is written, or immediately if the channel is already closed.</returns>
    public async Task BasicNackAsync(ulong deliveryTag, bool multiple, bool requeue, CancellationToken cancellationToken)
    {
        // Closing a channel requeues its outstanding unacknowledged deliveries, leaving nothing to nack.
        if (!_lifetime.TryLease(out var lease))
            return;

        using (lease)
        {
            try
            {
                await _channel.BasicNackAsync(deliveryTag, multiple, requeue, cancellationToken).ConfigureAwait(false);
            }
            catch (AlreadyClosedException)
            {
            }
        }
    }

    /// <summary>Starts the configured RabbitMQ consumer.</summary>
    /// <param name="queue">The source queue name.</param>
    /// <param name="noAck">Whether RabbitMQ should consider deliveries acknowledged immediately.</param>
    /// <param name="exclusive">Whether the broker permits only this consumer on the queue.</param>
    /// <param name="arguments">The consumer arguments.</param>
    /// <param name="consumer">The callback receiver for deliveries and lifecycle events.</param>
    /// <param name="consumerTag">The requested consumer tag, or an empty string for a generated tag.</param>
    /// <param name="cancellationToken">Cancellation for starting the consumer.</param>
    /// <returns>The consumer tag assigned by RabbitMQ.</returns>
    public async Task<string> BasicConsumeAsync(string queue, bool noAck, bool exclusive, IDictionary<string, object?> arguments,
        IAsyncBasicConsumer consumer, string consumerTag, CancellationToken cancellationToken)
    {
        using var lease = Lease();

        return await _channel.BasicConsumeAsync(queue, noAck, consumerTag, false, exclusive, arguments, consumer, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Cancels the active RabbitMQ consumer.</summary>
    /// <param name="consumerTag">The tag of the consumer to cancel.</param>
    /// <param name="cancellationToken">Cancellation for the broker command.</param>
    /// <returns>A task that completes when the cancel command has been sent.</returns>
    public async Task BasicCancelAsync(string consumerTag, CancellationToken cancellationToken)
    {
        using var lease = Lease();

        await _channel.BasicCancelAsync(consumerTag, false, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Schedules transport-agent shutdown after an unrecoverable channel fault.</summary>
    /// <param name="exception">The channel failure already reported by the caller.</param>
    /// <param name="inputAddress">The receive endpoint address affected by the fault.</param>
    public void NotifyFaulted(Exception exception, Uri inputAddress)
    {
        lock (_faultStopLock)
        {
            if (_faultStopTask == null || _faultStopTask.IsCompleted)
                _faultStopTask = StopAfterCallbackAsync(inputAddress);
        }
    }

    async Task StopAfterCallbackAsync(Uri inputAddress)
    {
        await Task.Yield();

        try
        {
            await _agent.StopAsync($"Unrecoverable exception on {inputAddress.GetEndpointName()}", CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception stopException)
        {
            LogContext.Error?.Log(stopException, "Stopping faulted RabbitMQ channel context failed: {InputAddress}", inputAddress);
        }
    }

    /// <summary>
    /// Claims the channel for one operation. Refusal preserves the broker shutdown reason retained
    /// by the transport lifetime.
    /// </summary>
    /// <returns>A lease that prevents channel disposal until released.</returns>
    TransportLifetime.Lease Lease()
    {
        if (_lifetime.TryLease(out var lease))
            return lease;

        throw _lifetime.NotAvailable();
    }

    /// <summary>Releases the resources owned by this instance.</summary>
    /// <returns>A task-like value that completes after in-flight leases finish and the channel is cleaned up.</returns>
    public async ValueTask DisposeAsync()
    {
        // Disposal waits for every leased channel operation to finish before releasing the channel.
        await _lifetime.DisposeAsync().ConfigureAwait(false);

        _tokenSource?.Dispose();
        _tokenSource = null;
    }
}
