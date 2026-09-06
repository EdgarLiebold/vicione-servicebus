using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Middleware.InMemoryOutbox;

/// <summary>Provides an endpoint for outbox send.</summary>
public class OutboxSendEndpoint :
    ITransportSendEndpoint
{
    readonly ITransportSendEndpoint _endpoint;
    readonly OutboxContext _outboxContext;

    /// <summary>Creates an send endpoint on the outbox.</summary>
    /// <param name="outboxContext">The outbox context for this consume operation.</param>
    /// <param name="endpoint">The actual endpoint returned by the transport.</param>
    public OutboxSendEndpoint(OutboxContext outboxContext, ISendEndpoint endpoint)
    {
        _outboxContext = outboxContext;
        _endpoint = endpoint as ITransportSendEndpoint ?? throw new ArgumentException("Must be a transport endpoint", nameof(endpoint));
    }

    /// <summary>The actual endpoint, wrapped by the outbox.</summary>
    public ISendEndpoint Endpoint => _endpoint;

    /// <summary>Connects send observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        return _endpoint.ConnectSendObserver(observer);
    }

    /// <summary>Creates send context.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the created value.</returns>
    public Task<SendContext<T>> CreateSendContextAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        return _endpoint.CreateSendContextAsync(message, pipe, cancellationToken);
    }

    Task ISendEndpoint.SendAsync<T>(T message, CancellationToken cancellationToken)
    {
        return DeferAsync(() => _endpoint.SendAsync(message, cancellationToken));
    }

    Task Advanced.IAdvancedSendEndpoint.SendAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
    {
        return DeferAsync(() => _endpoint.SendAsync(message, pipe, cancellationToken));
    }

    Task Advanced.IAdvancedSendEndpoint.SendAsync<T>(T message, IPipe<SendContext> pipe, CancellationToken cancellationToken)
    {
        return DeferAsync(() => _endpoint.SendAsync(message, pipe, cancellationToken));
    }

    Task Advanced.IAdvancedSendEndpoint.SendAsync(object message, CancellationToken cancellationToken)
    {
        return DeferAsync(() => _endpoint.SendAsync(message, cancellationToken));
    }

    Task Advanced.IAdvancedSendEndpoint.SendAsync(object message, Type messageType, CancellationToken cancellationToken)
    {
        return DeferAsync(() => _endpoint.SendAsync(message, messageType, cancellationToken));
    }

    Task Advanced.IAdvancedSendEndpoint.SendAsync(object message, IPipe<SendContext> pipe, CancellationToken cancellationToken)
    {
        return DeferAsync(() => _endpoint.SendAsync(message, pipe, cancellationToken));
    }

    Task Advanced.IAdvancedSendEndpoint.SendAsync(object message, Type messageType, IPipe<SendContext> pipe, CancellationToken cancellationToken)
    {
        return DeferAsync(() => _endpoint.SendAsync(message, messageType, pipe, cancellationToken));
    }

    Task Advanced.IAdvancedSendEndpoint.SendAsync<T>(object values, CancellationToken cancellationToken)
    {
        return DeferAsync(() => _endpoint.SendAsync<T>(values, cancellationToken));
    }

    Task Advanced.IAdvancedSendEndpoint.SendAsync<T>(object values, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
    {
        return DeferAsync(() => _endpoint.SendAsync(values, pipe, cancellationToken));
    }

    Task Advanced.IAdvancedSendEndpoint.SendAsync<T>(object values, IPipe<SendContext> pipe, CancellationToken cancellationToken)
    {
        return DeferAsync(() => _endpoint.SendAsync<T>(values, pipe, cancellationToken));
    }

    Task DeferAsync(Func<Task> send)
    {
        var enqueue = LogContext.Current?.StartOutboxEnqueueInstrument();
        try
        {
            Task pendingDelivery = _outboxContext.AddAsync(() => DeliverAsync(send));
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
        var delivery = LogContext.Current?.StartOutboxDeliveryInstrument();
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
