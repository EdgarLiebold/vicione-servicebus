using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Logging.Monitoring;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Middleware.InMemoryOutbox;

/// <summary>Defers transport sends until its in-memory outbox is released for delivery.</summary>
internal sealed class OutboxSendEndpoint :
    ITransportSendEndpoint
{
    readonly ITransportSendEndpoint _endpoint;
    readonly OutboxContext _outboxContext;

    /// <summary>Initializes an outbox endpoint over a transport endpoint.</summary>
    /// <param name="outboxContext">The outbox that owns deferred operations.</param>
    /// <param name="endpoint">The transport endpoint invoked after release.</param>
    public OutboxSendEndpoint(OutboxContext outboxContext, ISendEndpoint endpoint)
    {
        _outboxContext = outboxContext ?? throw new ArgumentNullException(nameof(outboxContext));
        ArgumentNullException.ThrowIfNull(endpoint);
        _endpoint = endpoint as ITransportSendEndpoint ?? throw new ArgumentException("Must be a transport endpoint", nameof(endpoint));
    }

    /// <summary>Gets the wrapped transport endpoint.</summary>
    public ISendEndpoint Endpoint => _endpoint;

    /// <summary>Registers an observer with the wrapped transport endpoint.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _endpoint.ConnectSendObserver(observer);
    }

    /// <summary>Creates a send context through the wrapped transport endpoint.</summary>
    /// <typeparam name="T">The outgoing message contract.</typeparam>
    /// <param name="message">The outgoing message.</param>
    /// <param name="pipe">The pipe that customizes the send context.</param>
    /// <param name="cancellationToken">The token that cancels context creation.</param>
    /// <returns>A task containing the configured send context.</returns>
    public Task<SendContext<T>> CreateSendContextAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);
        return _endpoint.CreateSendContextAsync(message, pipe, cancellationToken);
    }

    Task ISendEndpoint.SendAsync<T>(T message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        return DeferAsync(() => _endpoint.SendAsync(message, cancellationToken), cancellationToken);
    }

    Task Advanced.IAdvancedSendEndpoint.SendAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);
        return DeferAsync(() => _endpoint.SendAsync(message, pipe, cancellationToken), cancellationToken);
    }

    Task Advanced.IAdvancedSendEndpoint.SendAsync<T>(T message, IPipe<SendContext> pipe, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);
        return DeferAsync(() => _endpoint.SendAsync(message, pipe, cancellationToken), cancellationToken);
    }

    Task Advanced.IAdvancedSendEndpoint.SendAsync(object message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        return DeferAsync(() => _endpoint.SendAsync(message, cancellationToken), cancellationToken);
    }

    Task Advanced.IAdvancedSendEndpoint.SendAsync(object message, Type messageType, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);
        return DeferAsync(() => _endpoint.SendAsync(message, messageType, cancellationToken), cancellationToken);
    }

    Task Advanced.IAdvancedSendEndpoint.SendAsync(object message, IPipe<SendContext> pipe, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);
        return DeferAsync(() => _endpoint.SendAsync(message, pipe, cancellationToken), cancellationToken);
    }

    Task Advanced.IAdvancedSendEndpoint.SendAsync(object message, Type messageType, IPipe<SendContext> pipe, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentNullException.ThrowIfNull(pipe);
        return DeferAsync(() => _endpoint.SendAsync(message, messageType, pipe, cancellationToken), cancellationToken);
    }

    Task Advanced.IAdvancedSendEndpoint.SendAsync<T>(object values, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(values);
        return DeferAsync(() => _endpoint.SendAsync<T>(values, cancellationToken), cancellationToken);
    }

    Task Advanced.IAdvancedSendEndpoint.SendAsync<T>(object values, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(pipe);
        return DeferAsync(() => _endpoint.SendAsync(values, pipe, cancellationToken), cancellationToken);
    }

    Task Advanced.IAdvancedSendEndpoint.SendAsync<T>(object values, IPipe<SendContext> pipe, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(pipe);
        return DeferAsync(() => _endpoint.SendAsync<T>(values, pipe, cancellationToken), cancellationToken);
    }

    Task DeferAsync(Func<Task> send, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(send);
        var enqueue = LogContext.Current?.TryStartOutboxEnqueueMetrics();
        try
        {
            Task pendingDelivery = _outboxContext.AddAsync(() => DeliverAsync(send), cancellationToken);
            enqueue?.Complete();
            return pendingDelivery;
        }
        catch (Exception exception)
        {
            enqueue?.RecordException(exception);
            enqueue?.Complete();
            throw;
        }
    }

    static async Task DeliverAsync(Func<Task> send)
    {
        var delivery = LogContext.Current?.TryStartOutboxDeliveryMetrics();
        try
        {
            await send().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            delivery?.RecordException(exception);
            throw;
        }
        finally
        {
            delivery?.Complete();
        }
    }
}
