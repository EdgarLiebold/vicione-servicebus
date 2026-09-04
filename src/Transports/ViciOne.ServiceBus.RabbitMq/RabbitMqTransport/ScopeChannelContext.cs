using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Provides a scope channel context implementation.
/// </summary>
public class ScopeChannelContext :
    ScopePipeContext,
    ChannelContext,
    IDisposable
{
    readonly CancellationToken _cancellationToken;
    readonly ChannelContext _context;
    CancellationTokenSource? _tokenSource;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public ScopeChannelContext(ChannelContext context, CancellationToken cancellationToken)
        : base(context)
    {
        _context = context;

        _cancellationToken = cancellationToken;
        _tokenSource = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken, cancellationToken);
    }

    /// <summary>
    /// Gets the cancellation token value.
    /// </summary>
    public override CancellationToken CancellationToken => _tokenSource?.Token ?? _cancellationToken;

    /// <summary>
    /// Gets the channel value.
    /// </summary>
    public IChannel Channel => _context.Channel;

    /// <summary>
    /// Gets the connection context value.
    /// </summary>
    public ConnectionContext ConnectionContext => _context.ConnectionContext;

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
    public async Task BasicPublishAsync(string exchange, string routingKey, bool mandatory, BasicProperties basicProperties, byte[] body, bool awaitAck,
        CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.BasicPublishAsync(exchange, routingKey, mandatory, basicProperties, body, awaitAck, tokenSource.Token).ConfigureAwait(false);
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
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.ExchangeBindAsync(destination, source, routingKey, arguments, tokenSource.Token).ConfigureAwait(false);
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
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.ExchangeDeclareAsync(exchange, type, durable, autoDelete, arguments, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the exchange declare passive operation.
    /// </summary>
    /// <param name="exchange">The exchange value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task ExchangeDeclarePassiveAsync(string exchange, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.ExchangeDeclarePassiveAsync(exchange, tokenSource.Token).ConfigureAwait(false);
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
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.QueueBindAsync(queue, exchange, routingKey, arguments, tokenSource.Token).ConfigureAwait(false);
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
    public async Task<QueueDeclareOk> QueueDeclareAsync(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object?> arguments,
        CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.QueueDeclareAsync(queue, durable, exclusive, autoDelete, arguments, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the queue declare passive operation.
    /// </summary>
    /// <param name="queue">The queue value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<QueueDeclareOk> QueueDeclarePassiveAsync(string queue, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.QueueDeclarePassiveAsync(queue, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the queue purge operation.
    /// </summary>
    /// <param name="queue">The queue value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<uint> QueuePurgeAsync(string queue, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.QueuePurgeAsync(queue, tokenSource.Token).ConfigureAwait(false);
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
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.BasicQosAsync(prefetchSize, prefetchCount, global, tokenSource.Token).ConfigureAwait(false);
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
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.BasicAckAsync(deliveryTag, multiple, tokenSource.Token).ConfigureAwait(false);
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
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.BasicNackAsync(deliveryTag, multiple, requeue, tokenSource.Token).ConfigureAwait(false);
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
    public async Task<string> BasicConsumeAsync(string queue, bool noAck, bool exclusive, IDictionary<string, object?> arguments, IAsyncBasicConsumer consumer,
        string consumerTag, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.BasicConsumeAsync(queue, noAck, exclusive, arguments, consumer, consumerTag, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the basic cancel operation.
    /// </summary>
    /// <param name="consumerTag">The consumer tag value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task BasicCancelAsync(string consumerTag, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.BasicCancelAsync(consumerTag, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the notify faulted operation.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="contextInputAddress">The context input address value.</param>
    public void NotifyFaulted(Exception exception, Uri contextInputAddress)
    {
        _context.NotifyFaulted(exception, contextInputAddress);
    }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    public void Dispose()
    {
        _tokenSource?.Dispose();
        _tokenSource = null;
    }
}
