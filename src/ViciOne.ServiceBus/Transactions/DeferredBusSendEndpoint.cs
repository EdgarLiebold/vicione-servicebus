using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Transactions;

internal sealed class DeferredBusSendEndpoint :
    ITransportSendEndpoint
{
    readonly DeferredBus _deferredBus;
    readonly ITransportSendEndpoint _endpoint;

    public DeferredBusSendEndpoint(DeferredBus deferredBus, ISendEndpoint endpoint)
    {
        _deferredBus = deferredBus ?? throw new ArgumentNullException(nameof(deferredBus));
        _endpoint = endpoint as ITransportSendEndpoint ?? throw new ArgumentException("Must be a transport endpoint", nameof(endpoint));
    }

    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        return _endpoint.ConnectSendObserver(observer);
    }

    public Task<SendContext<T>> CreateSendContextAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        return _endpoint.CreateSendContextAsync(message, pipe, cancellationToken);
    }

    public Task SendAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class
    {
        return _deferredBus.AddAsync(token => _endpoint.SendAsync(message, token), cancellationToken);
    }

    public Task SendAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        return _deferredBus.AddAsync(token => _endpoint.SendAsync(message, pipe, token), cancellationToken);
    }

    public Task SendAsync<T>(T message, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        return _deferredBus.AddAsync(token => _endpoint.SendAsync(message, pipe, token), cancellationToken);
    }

    public Task SendAsync(object message, CancellationToken cancellationToken = default)
    {
        return _deferredBus.AddAsync(token => _endpoint.SendAsync(message, token), cancellationToken);
    }

    public Task SendAsync(object message, Type messageType, CancellationToken cancellationToken = default)
    {
        return _deferredBus.AddAsync(token => _endpoint.SendAsync(message, messageType, token), cancellationToken);
    }

    public Task SendAsync(object message, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
    {
        return _deferredBus.AddAsync(token => _endpoint.SendAsync(message, pipe, token), cancellationToken);
    }

    public Task SendAsync(object message, Type messageType, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
    {
        return _deferredBus.AddAsync(token => _endpoint.SendAsync(message, messageType, pipe, token), cancellationToken);
    }

    public Task SendAsync<T>(object values, CancellationToken cancellationToken = default)
        where T : class
    {
        return _deferredBus.AddAsync(token => _endpoint.SendAsync<T>(values, token), cancellationToken);
    }

    public Task SendAsync<T>(object values, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        return _deferredBus.AddAsync(token => _endpoint.SendAsync(values, pipe, token), cancellationToken);
    }

    public Task SendAsync<T>(object values, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        return _deferredBus.AddAsync(token => _endpoint.SendAsync<T>(values, pipe, token), cancellationToken);
    }
}
