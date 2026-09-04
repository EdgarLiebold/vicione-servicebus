using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Middleware.InMemoryOutbox;

public class OutboxSendEndpoint :
    ITransportSendEndpoint
{
    readonly ITransportSendEndpoint _endpoint;
    readonly OutboxContext _outboxContext;

    /// <summary>
    /// Creates an send endpoint on the outbox
    /// </summary>
    /// <param name="outboxContext">The outbox context for this consume operation</param>
    /// <param name="endpoint">The actual endpoint returned by the transport</param>
    public OutboxSendEndpoint(OutboxContext outboxContext, ISendEndpoint endpoint)
    {
        _outboxContext = outboxContext;
        _endpoint = endpoint as ITransportSendEndpoint ?? throw new ArgumentException("Must be a transport endpoint", nameof(endpoint));
    }

    /// <summary>
    /// The actual endpoint, wrapped by the outbox
    /// </summary>
    public ISendEndpoint Endpoint => _endpoint;

    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        return _endpoint.ConnectSendObserver(observer);
    }

    public Task<SendContext<T>> CreateSendContext<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        return _endpoint.CreateSendContext(message, pipe, cancellationToken);
    }

    Task ISendEndpoint.Send<T>(T message, CancellationToken cancellationToken)
    {
        return Defer(() => _endpoint.Send(message, cancellationToken));
    }

    Task ISendEndpoint.Send<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
    {
        return Defer(() => _endpoint.Send(message, pipe, cancellationToken));
    }

    Task ISendEndpoint.Send<T>(T message, IPipe<SendContext> pipe, CancellationToken cancellationToken)
    {
        return Defer(() => _endpoint.Send(message, pipe, cancellationToken));
    }

    Task ISendEndpoint.Send(object message, CancellationToken cancellationToken)
    {
        return Defer(() => _endpoint.Send(message, cancellationToken));
    }

    Task ISendEndpoint.Send(object message, Type messageType, CancellationToken cancellationToken)
    {
        return Defer(() => _endpoint.Send(message, messageType, cancellationToken));
    }

    Task ISendEndpoint.Send(object message, IPipe<SendContext> pipe, CancellationToken cancellationToken)
    {
        return Defer(() => _endpoint.Send(message, pipe, cancellationToken));
    }

    Task ISendEndpoint.Send(object message, Type messageType, IPipe<SendContext> pipe, CancellationToken cancellationToken)
    {
        return Defer(() => _endpoint.Send(message, messageType, pipe, cancellationToken));
    }

    Task ISendEndpoint.Send<T>(object values, CancellationToken cancellationToken)
    {
        return Defer(() => _endpoint.Send<T>(values, cancellationToken));
    }

    Task ISendEndpoint.Send<T>(object values, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
    {
        return Defer(() => _endpoint.Send(values, pipe, cancellationToken));
    }

    Task ISendEndpoint.Send<T>(object values, IPipe<SendContext> pipe, CancellationToken cancellationToken)
    {
        return Defer(() => _endpoint.Send<T>(values, pipe, cancellationToken));
    }

    Task Defer(Func<Task> send)
    {
        var enqueue = LogContext.Current?.StartOutboxEnqueueInstrument();
        try
        {
            Task pendingDelivery = _outboxContext.Add(() => Deliver(send));
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

    static async Task Deliver(Func<Task> send)
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
