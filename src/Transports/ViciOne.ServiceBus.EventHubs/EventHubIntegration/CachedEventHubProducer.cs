using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Caching;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Provides a cached event hub producer implementation.
/// </summary>
/// <typeparam name="TKey">The t key type.</typeparam>
public class CachedEventHubProducer<TKey> :
    IEventHubProducer,
    IResourceUsageSource,
    IAsyncDisposable
    where TKey : notnull
{
    readonly IEventHubProducer _producer;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="producer">The producer value.</param>
    public CachedEventHubProducer(TKey key, IEventHubProducer producer)
    {
        Key = key;
        _producer = producer;
    }

    /// <summary>
    /// Gets the key value.
    /// </summary>
    public TKey Key { get; }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public ValueTask DisposeAsync()
    {
        return _producer switch
        {
            IAsyncDisposable disposable => disposable.DisposeAsync(),
            _ => default
        };
    }

    /// <summary>
    /// Connects send observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        Used?.Invoke();
        return _producer.ConnectSendObserver(observer);
    }

    /// <summary>
    /// Performs the produce operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task ProduceAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class
    {
        Used?.Invoke();
        return _producer.ProduceAsync(message, cancellationToken);
    }

    /// <summary>
    /// Performs the produce operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="messages">The messages value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task ProduceAsync<T>(IEnumerable<T> messages, CancellationToken cancellationToken = default)
        where T : class
    {
        Used?.Invoke();
        return _producer.ProduceAsync(messages, cancellationToken);
    }

    /// <summary>
    /// Performs the produce operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task ProduceAsync<T>(T message, IPipe<EventHubSendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        Used?.Invoke();
        return _producer.ProduceAsync(message, pipe, cancellationToken);
    }

    /// <summary>
    /// Performs the produce operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="messages">The messages value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task ProduceAsync<T>(IEnumerable<T> messages, IPipe<EventHubSendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        Used?.Invoke();
        return _producer.ProduceAsync(messages, pipe, cancellationToken);
    }

    /// <summary>
    /// Performs the produce operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="values">The values value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task ProduceAsync<T>(object values, CancellationToken cancellationToken = default)
        where T : class
    {
        Used?.Invoke();
        return _producer.ProduceAsync<T>(values, cancellationToken);
    }

    /// <summary>
    /// Performs the produce operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="values">The values value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task ProduceAsync<T>(IEnumerable<object> values, CancellationToken cancellationToken = default)
        where T : class
    {
        Used?.Invoke();
        return _producer.ProduceAsync<T>(values, cancellationToken);
    }

    /// <summary>
    /// Performs the produce operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="values">The values value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task ProduceAsync<T>(object values, IPipe<EventHubSendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        Used?.Invoke();
        return _producer.ProduceAsync(values, pipe, cancellationToken);
    }

    /// <summary>
    /// Performs the produce operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="values">The values value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task ProduceAsync<T>(IEnumerable<object> values, IPipe<EventHubSendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        Used?.Invoke();
        return _producer.ProduceAsync(values, pipe, cancellationToken);
    }

    /// <summary>
    /// Occurs when used.
    /// </summary>
    public event Action? Used;
}
