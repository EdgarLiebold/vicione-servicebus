using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Caching;

namespace ViciOne.ServiceBus.Transports;

public class CachedSendEndpoint<TKey> :
    ITransportSendEndpoint,
    IResourceUsageSource,
    IAsyncDisposable
{
    readonly ITransportSendEndpoint _endpoint;

    public CachedSendEndpoint(TKey key, ISendEndpoint endpoint)
    {
        Key = key;
        _endpoint = endpoint as ITransportSendEndpoint ?? throw new ArgumentException("Must be a transport endpoint", nameof(endpoint));
    }

    public TKey Key { get; }

    ValueTask IAsyncDisposable.DisposeAsync()
    {
        return _endpoint switch
        {
            IAsyncDisposable disposable => disposable.DisposeAsync(),
            _ => default
        };
    }

    public event Action? Used;

    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        Used?.Invoke();
        return _endpoint.ConnectSendObserver(observer);
    }

    public Task<SendContext<T>> CreateSendContextAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        return _endpoint.CreateSendContextAsync(message, pipe, cancellationToken);
    }

    public Task SendAsync<T>(T message, CancellationToken cancellationToken = new CancellationToken())
        where T : class
    {
        Used?.Invoke();
        return _endpoint.SendAsync(message, cancellationToken);
    }

    public Task SendAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = new CancellationToken())
        where T : class
    {
        Used?.Invoke();
        return _endpoint.SendAsync(message, pipe, cancellationToken);
    }

    public Task SendAsync<T>(T message, IPipe<SendContext> pipe, CancellationToken cancellationToken = new CancellationToken())
        where T : class
    {
        Used?.Invoke();
        return _endpoint.SendAsync(message, pipe, cancellationToken);
    }

    public Task SendAsync(object message, CancellationToken cancellationToken = new CancellationToken())
    {
        Used?.Invoke();
        return _endpoint.SendAsync(message, cancellationToken);
    }

    public Task SendAsync(object message, Type messageType, CancellationToken cancellationToken = new CancellationToken())
    {
        Used?.Invoke();
        return _endpoint.SendAsync(message, messageType, cancellationToken);
    }

    public Task SendAsync(object message, IPipe<SendContext> pipe, CancellationToken cancellationToken = new CancellationToken())
    {
        Used?.Invoke();
        return _endpoint.SendAsync(message, pipe, cancellationToken);
    }

    public Task SendAsync(object message, Type messageType, IPipe<SendContext> pipe, CancellationToken cancellationToken = new CancellationToken())
    {
        Used?.Invoke();
        return _endpoint.SendAsync(message, messageType, pipe, cancellationToken);
    }

    public Task SendAsync<T>(object values, CancellationToken cancellationToken = new CancellationToken())
        where T : class
    {
        Used?.Invoke();
        return _endpoint.SendAsync<T>(values, cancellationToken);
    }

    public Task SendAsync<T>(object values, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = new CancellationToken())
        where T : class
    {
        Used?.Invoke();
        return _endpoint.SendAsync(values, pipe, cancellationToken);
    }

    public Task SendAsync<T>(object values, IPipe<SendContext> pipe, CancellationToken cancellationToken = new CancellationToken())
        where T : class
    {
        Used?.Invoke();
        return _endpoint.SendAsync<T>(values, pipe, cancellationToken);
    }
}
