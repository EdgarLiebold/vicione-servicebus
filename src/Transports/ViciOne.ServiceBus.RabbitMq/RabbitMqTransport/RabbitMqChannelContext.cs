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

/// <summary>
/// Provides a rabbit mq channel context implementation.
/// </summary>
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
    /// Owns the channel. Every operation below runs under a lease from it, so the channel is not
    /// disposed while one is still unwinding — which is what used to replace the broker's answer
    /// with an ObjectDisposedException.
    /// </summary>
    readonly TransportLifetime _lifetime;

    Task? _faultStopTask;
    CancellationTokenSource? _tokenSource;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="connectionContext">The connection context value.</param>
    /// <param name="channel">The channel value.</param>
    /// <param name="agent">The agent value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
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

    /// <summary>
    /// Gets the cancellation token value.
    /// </summary>
    public override CancellationToken CancellationToken => _tokenSource?.Token ?? _cancellationToken;

    /// <summary>
    /// Gets the channel value.
    /// </summary>
    public IChannel Channel => _channel;

    internal TransportLifetime Lifetime => _lifetime;

    /// <summary>
    /// Gets the connection context value.
    /// </summary>
    public ConnectionContext ConnectionContext { get; }

    /// <summary>
    /// Performs the basic publish operation.
    /// </summary>
    /// <param name="exchange">The exchange value.</param>
    /// <param name="routingKey">The routing key value.</param>
    /// <param name="mandatory">The mandatory value.</param>
    /// <param name="basicProperties">The basic properties value.</param>
    /// <param name="body">The body value.</param>
    /// <param name="awaitAck">The await ack value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task BasicPublishAsync(string exchange, string routingKey, bool mandatory, BasicProperties basicProperties, byte[] body, bool awaitAck,
        CancellationToken cancellationToken)
    {
        // The lease is held until the client's own task finishes, not until this method returns.
        // With publisher confirms off the caller does not wait for the broker, but the client still
        // does — and releasing before that would let the channel be disposed underneath it, which is
        // the very race this ownership exists to prevent. The task is observed on both paths, so a
        // publish nobody waits for still cannot become an unobserved exception.
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


    /// <summary>
    /// Performs the exchange bind operation.
    /// </summary>
    /// <param name="destination">The destination value.</param>
    /// <param name="source">The source value.</param>
    /// <param name="routingKey">The routing key value.</param>
    /// <param name="arguments">The arguments value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task ExchangeBindAsync(string destination, string source, string routingKey, IDictionary<string, object?> arguments,
        CancellationToken cancellationToken)
    {
        using var lease = Lease();

        await _channel.ExchangeBindAsync(destination, source, routingKey, arguments, false, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the exchange declare operation.
    /// </summary>
    /// <param name="exchange">The exchange value.</param>
    /// <param name="type">The type value.</param>
    /// <param name="durable">The durable value.</param>
    /// <param name="autoDelete">The auto delete value.</param>
    /// <param name="arguments">The arguments value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task ExchangeDeclareAsync(string exchange, string type, bool durable, bool autoDelete, IDictionary<string, object?> arguments,
        CancellationToken cancellationToken)
    {
        using var lease = Lease();

        await _channel.ExchangeDeclareAsync(exchange, type, durable, autoDelete, arguments, false, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the exchange declare passive operation.
    /// </summary>
    /// <param name="exchange">The exchange value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task ExchangeDeclarePassiveAsync(string exchange, CancellationToken cancellationToken)
    {
        using var lease = Lease();

        await _channel.ExchangeDeclarePassiveAsync(exchange, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the queue bind operation.
    /// </summary>
    /// <param name="queue">The queue value.</param>
    /// <param name="exchange">The exchange value.</param>
    /// <param name="routingKey">The routing key value.</param>
    /// <param name="arguments">The arguments value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task QueueBindAsync(string queue, string exchange, string routingKey, IDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        using var lease = Lease();

        await _channel.QueueBindAsync(queue, exchange, routingKey, arguments, false, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the queue declare operation.
    /// </summary>
    /// <param name="queue">The queue value.</param>
    /// <param name="durable">The durable value.</param>
    /// <param name="exclusive">The exclusive value.</param>
    /// <param name="autoDelete">The auto delete value.</param>
    /// <param name="arguments">The arguments value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<QueueDeclareOk> QueueDeclareAsync(string queue, bool durable, bool exclusive, bool autoDelete,
        IDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        using var lease = Lease();

        return await _channel.QueueDeclareAsync(queue, durable, exclusive, autoDelete, arguments, false, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the queue declare passive operation.
    /// </summary>
    /// <param name="queue">The queue value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<QueueDeclareOk> QueueDeclarePassiveAsync(string queue, CancellationToken cancellationToken)
    {
        using var lease = Lease();

        return await _channel.QueueDeclarePassiveAsync(queue, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the queue purge operation.
    /// </summary>
    /// <param name="queue">The queue value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<uint> QueuePurgeAsync(string queue, CancellationToken cancellationToken)
    {
        using var lease = Lease();

        return await _channel.QueuePurgeAsync(queue, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the basic qos operation.
    /// </summary>
    /// <param name="prefetchSize">The prefetch size value.</param>
    /// <param name="prefetchCount">The prefetch count value.</param>
    /// <param name="global">The global value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task BasicQosAsync(uint prefetchSize, ushort prefetchCount, bool global, CancellationToken cancellationToken)
    {
        using var lease = Lease();

        await _channel.BasicQosAsync(prefetchSize, prefetchCount, global, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the basic ack operation.
    /// </summary>
    /// <param name="deliveryTag">The delivery tag value.</param>
    /// <param name="multiple">The multiple value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async ValueTask BasicAckAsync(ulong deliveryTag, bool multiple, CancellationToken cancellationToken)
    {
        // Preserve the broker or transport exception so the caller receives the authoritative
        // acknowledgement failure.
        using var lease = Lease();

        await _channel.BasicAckAsync(deliveryTag, multiple, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the basic nack operation.
    /// </summary>
    /// <param name="deliveryTag">The delivery tag value.</param>
    /// <param name="multiple">The multiple value.</param>
    /// <param name="requeue">The requeue value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task BasicNackAsync(ulong deliveryTag, bool multiple, bool requeue, CancellationToken cancellationToken)
    {
        // A nack on a channel that is finished is not an error: shutting down, the broker requeues
        // the prefetched messages anyway. So the refused lease is answered with silence here, unlike
        // an acknowledgement, where the caller has to learn that it did not happen.
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

    /// <summary>
    /// Performs the basic consume operation.
    /// </summary>
    /// <param name="queue">The queue value.</param>
    /// <param name="noAck">The no ack value.</param>
    /// <param name="exclusive">The exclusive value.</param>
    /// <param name="arguments">The arguments value.</param>
    /// <param name="consumer">The consumer value.</param>
    /// <param name="consumerTag">The consumer tag value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<string> BasicConsumeAsync(string queue, bool noAck, bool exclusive, IDictionary<string, object?> arguments,
        IAsyncBasicConsumer consumer, string consumerTag, CancellationToken cancellationToken)
    {
        using var lease = Lease();

        return await _channel.BasicConsumeAsync(queue, noAck, consumerTag, false, exclusive, arguments, consumer, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the basic cancel operation.
    /// </summary>
    /// <param name="consumerTag">The consumer tag value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task BasicCancelAsync(string consumerTag, CancellationToken cancellationToken)
    {
        using var lease = Lease();

        await _channel.BasicCancelAsync(consumerTag, false, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the notify faulted operation.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="inputAddress">The input address value.</param>
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
    /// Claims the channel for one operation, or says why it is gone.
    /// <para>
    /// The refusal carries the broker's own close reason when there is one. That is what keeping it
    /// is for: a caller arriving after the channel closed learns what closed it, rather than an
    /// invented answer or an ObjectDisposedException from a channel pulled out from under it.
    /// </para>
    /// </summary>
    TransportLifetime.Lease Lease()
    {
        if (_lifetime.TryLease(out var lease))
            return lease;

        throw _lifetime.NotAvailable();
    }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public async ValueTask DisposeAsync()
    {
        // Waits for the operations still running. Disposing while one of them is unwinding is the
        // defect this ownership exists to prevent.
        await _lifetime.DisposeAsync().ConfigureAwait(false);

        _tokenSource?.Dispose();
        _tokenSource = null;
    }
}
