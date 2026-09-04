using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Caching;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Provides a cached send endpoint implementation.
/// </summary>
/// <typeparam name="TKey">The t key type.</typeparam>
public class CachedSendEndpoint<TKey> :
    ITransportSendEndpoint,
    IResourceUsageSource,
    IAsyncDisposable
{
    readonly ITransportSendEndpoint _endpoint;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="endpoint">The endpoint value.</param>
    public CachedSendEndpoint(TKey key, ISendEndpoint endpoint)
    {
        Key = key;
        _endpoint = endpoint as ITransportSendEndpoint ?? throw new ArgumentException("Must be a transport endpoint", nameof(endpoint));
    }

    /// <summary>
    /// Gets the key value.
    /// </summary>
    public TKey Key { get; }

    ValueTask IAsyncDisposable.DisposeAsync()
    {
        return _endpoint switch
        {
            IAsyncDisposable disposable => disposable.DisposeAsync(),
            _ => default
        };
    }

    /// <summary>
    /// Occurs when used.
    /// </summary>
    public event Action? Used;

    /// <summary>
    /// Connects send observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        Used?.Invoke();
        return _endpoint.ConnectSendObserver(observer);
    }

    /// <summary>
    /// Creates send context.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<SendContext<T>> CreateSendContextAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        return _endpoint.CreateSendContextAsync(message, pipe, cancellationToken);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync<T>(T message, CancellationToken cancellationToken = new CancellationToken())
        where T : class
    {
        Used?.Invoke();
        return _endpoint.SendAsync(message, cancellationToken);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = new CancellationToken())
        where T : class
    {
        Used?.Invoke();
        return _endpoint.SendAsync(message, pipe, cancellationToken);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync<T>(T message, IPipe<SendContext> pipe, CancellationToken cancellationToken = new CancellationToken())
        where T : class
    {
        Used?.Invoke();
        return _endpoint.SendAsync(message, pipe, cancellationToken);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(object message, CancellationToken cancellationToken = new CancellationToken())
    {
        Used?.Invoke();
        return _endpoint.SendAsync(message, cancellationToken);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="messageType">The message type value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(object message, Type messageType, CancellationToken cancellationToken = new CancellationToken())
    {
        Used?.Invoke();
        return _endpoint.SendAsync(message, messageType, cancellationToken);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(object message, IPipe<SendContext> pipe, CancellationToken cancellationToken = new CancellationToken())
    {
        Used?.Invoke();
        return _endpoint.SendAsync(message, pipe, cancellationToken);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="messageType">The message type value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(object message, Type messageType, IPipe<SendContext> pipe, CancellationToken cancellationToken = new CancellationToken())
    {
        Used?.Invoke();
        return _endpoint.SendAsync(message, messageType, pipe, cancellationToken);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="values">The values value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync<T>(object values, CancellationToken cancellationToken = new CancellationToken())
        where T : class
    {
        Used?.Invoke();
        return _endpoint.SendAsync<T>(values, cancellationToken);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="values">The values value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync<T>(object values, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = new CancellationToken())
        where T : class
    {
        Used?.Invoke();
        return _endpoint.SendAsync(values, pipe, cancellationToken);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="values">The values value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync<T>(object values, IPipe<SendContext> pipe, CancellationToken cancellationToken = new CancellationToken())
        where T : class
    {
        Used?.Invoke();
        return _endpoint.SendAsync<T>(values, pipe, cancellationToken);
    }
}
