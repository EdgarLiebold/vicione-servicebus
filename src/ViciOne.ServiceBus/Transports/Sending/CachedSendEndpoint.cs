using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Caching;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Tracks cache usage while forwarding operations to an owned transport send endpoint.</summary>
/// <typeparam name="TKey">The normalized cache key that owns the endpoint.</typeparam>
internal sealed class CachedSendEndpoint<TKey> :
    ITransportSendEndpoint,
    IResourceUsageSource,
    IAsyncDisposable
    where TKey : notnull
{
    readonly ITransportSendEndpoint _endpoint;

    /// <summary>Initializes a usage-reporting wrapper around a transport send endpoint.</summary>
    /// <param name="key">The normalized key used by the owning cache.</param>
    /// <param name="endpoint">The transport send endpoint owned by the cache entry.</param>
    internal CachedSendEndpoint(TKey key, ISendEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(endpoint);

        Key = key;
        _endpoint = endpoint as ITransportSendEndpoint
            ?? throw new ArgumentException("The endpoint must implement ITransportSendEndpoint.", nameof(endpoint));
    }

    /// <summary>Gets the normalized key owned by this cache entry.</summary>
    public TKey Key { get; }

    ValueTask IAsyncDisposable.DisposeAsync()
    {
        return _endpoint switch
        {
            IAsyncDisposable disposable => disposable.DisposeAsync(),
            _ => default
        };
    }

    /// <summary>Occurs when an operation uses the cached endpoint.</summary>
    public event Action? Used;

    /// <inheritdoc />
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        Used?.Invoke();
        return _endpoint.ConnectSendObserver(observer);
    }

    /// <inheritdoc />
    public Task<SendContext<T>> CreateSendContextAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        Used?.Invoke();
        return _endpoint.CreateSendContextAsync(message, pipe, cancellationToken);
    }

    /// <inheritdoc />
    public Task SendAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class
    {
        Used?.Invoke();
        return _endpoint.SendAsync(message, cancellationToken);
    }

    /// <inheritdoc />
    public Task SendAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        Used?.Invoke();
        return _endpoint.SendAsync(message, pipe, cancellationToken);
    }

    /// <inheritdoc />
    public Task SendAsync<T>(T message, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        Used?.Invoke();
        return _endpoint.SendAsync(message, pipe, cancellationToken);
    }

    /// <inheritdoc />
    public Task SendAsync(object message, CancellationToken cancellationToken = default)
    {
        Used?.Invoke();
        return _endpoint.SendAsync(message, cancellationToken);
    }

    /// <inheritdoc />
    public Task SendAsync(object message, Type messageType, CancellationToken cancellationToken = default)
    {
        Used?.Invoke();
        return _endpoint.SendAsync(message, messageType, cancellationToken);
    }

    /// <inheritdoc />
    public Task SendAsync(object message, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
    {
        Used?.Invoke();
        return _endpoint.SendAsync(message, pipe, cancellationToken);
    }

    /// <inheritdoc />
    public Task SendAsync(object message, Type messageType, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
    {
        Used?.Invoke();
        return _endpoint.SendAsync(message, messageType, pipe, cancellationToken);
    }

    /// <inheritdoc />
    public Task SendAsync<T>(object values, CancellationToken cancellationToken = default)
        where T : class
    {
        Used?.Invoke();
        return _endpoint.SendAsync<T>(values, cancellationToken);
    }

    /// <inheritdoc />
    public Task SendAsync<T>(object values, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        Used?.Invoke();
        return _endpoint.SendAsync(values, pipe, cancellationToken);
    }

    /// <inheritdoc />
    public Task SendAsync<T>(object values, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        Used?.Invoke();
        return _endpoint.SendAsync<T>(values, pipe, cancellationToken);
    }
}
