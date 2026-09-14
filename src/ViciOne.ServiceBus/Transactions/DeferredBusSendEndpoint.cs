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
        ArgumentNullException.ThrowIfNull(endpoint);
        _endpoint = endpoint as ITransportSendEndpoint
            ?? throw new ArgumentException("The endpoint must expose transport send operations.", nameof(endpoint));
    }

    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _endpoint.ConnectSendObserver(observer);
    }

    public Task<SendContext<TMessage>> CreateSendContextAsync<TMessage>(TMessage message, IPipe<SendContext<TMessage>> pipe,
        CancellationToken cancellationToken)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);
        return _endpoint.CreateSendContextAsync(message, pipe, cancellationToken);
    }

    public Task SendAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);
        return _deferredBus.AddAsync(token => _endpoint.SendAsync(message, token), cancellationToken);
    }

    public Task SendAsync<TMessage>(TMessage message, IPipe<SendContext<TMessage>> pipe, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);
        return _deferredBus.AddAsync(token => _endpoint.SendAsync(message, pipe, token), cancellationToken);
    }

    public Task SendAsync<TMessage>(TMessage message, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);
        return _deferredBus.AddAsync(token => _endpoint.SendAsync(message, pipe, token), cancellationToken);
    }

    public Task SendAsync(object message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        return _deferredBus.AddAsync(token => _endpoint.SendAsync(message, token), cancellationToken);
    }

    public Task SendAsync(object message, Type messageType, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);
        return _deferredBus.AddAsync(token => _endpoint.SendAsync(message, messageType, token), cancellationToken);
    }

    public Task SendAsync(object message, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);
        return _deferredBus.AddAsync(token => _endpoint.SendAsync(message, pipe, token), cancellationToken);
    }

    public Task SendAsync(object message, Type messageType, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentNullException.ThrowIfNull(pipe);
        return _deferredBus.AddAsync(token => _endpoint.SendAsync(message, messageType, pipe, token), cancellationToken);
    }

    public Task SendAsync<TMessage>(object values, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(values);
        return _deferredBus.AddAsync(token => _endpoint.SendAsync<TMessage>(values, token), cancellationToken);
    }

    public Task SendAsync<TMessage>(object values, IPipe<SendContext<TMessage>> pipe, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(pipe);
        return _deferredBus.AddAsync(token => _endpoint.SendAsync(values, pipe, token), cancellationToken);
    }

    public Task SendAsync<TMessage>(object values, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(pipe);
        return _deferredBus.AddAsync(token => _endpoint.SendAsync<TMessage>(values, pipe, token), cancellationToken);
    }
}
