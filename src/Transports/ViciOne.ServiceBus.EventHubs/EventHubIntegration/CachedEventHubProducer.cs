using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Caching;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Associates a cache key with an Event Hubs producer and reports each use to the resource cache.</summary>
/// <typeparam name="TKey">The producer cache-key type.</typeparam>
public class CachedEventHubProducer<TKey> :
    IEventHubProducer,
    IResourceUsageSource,
    IAsyncDisposable
    where TKey : notnull
{
    readonly IEventHubProducer _producer;

    /// <summary>Creates a cache-owned wrapper around a producer.</summary>
    /// <param name="key">The key that identifies the cached producer.</param>
    /// <param name="producer">The producer to wrap and dispose.</param>
    public CachedEventHubProducer(TKey key, IEventHubProducer producer)
    {
        Key = key;
        _producer = producer;
    }

    /// <summary>Gets the key used by the resource cache.</summary>
    public TKey Key { get; }

    /// <summary>Disposes the wrapped producer when it supports asynchronous disposal.</summary>
    /// <returns>The producer's disposal task, or a completed value task.</returns>
    public ValueTask DisposeAsync()
    {
        return _producer switch
        {
            IAsyncDisposable disposable => disposable.DisposeAsync(),
            _ => default
        };
    }

    /// <summary>Reports cache usage and connects a send observer to the wrapped producer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        Used?.Invoke();
        return _producer.ConnectSendObserver(observer);
    }

    /// <summary>Reports cache usage and produces one message.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="message">The message to produce.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The task returned by the wrapped producer.</returns>
    public Task ProduceAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class
    {
        Used?.Invoke();
        return _producer.ProduceAsync(message, cancellationToken);
    }

    /// <summary>Reports cache usage and produces a batch of messages.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="messages">The messages to produce.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The task returned by the wrapped producer.</returns>
    public Task ProduceAsync<T>(IEnumerable<T> messages, CancellationToken cancellationToken = default)
        where T : class
    {
        Used?.Invoke();
        return _producer.ProduceAsync(messages, cancellationToken);
    }

    /// <summary>Reports cache usage and produces one configured message.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="message">The message to produce.</param>
    /// <param name="pipe">The pipe that configures the Event Hubs send context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The task returned by the wrapped producer.</returns>
    public Task ProduceAsync<T>(T message, IPipe<EventHubSendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        Used?.Invoke();
        return _producer.ProduceAsync(message, pipe, cancellationToken);
    }

    /// <summary>Reports cache usage and produces a configured batch of messages.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="messages">The messages to produce.</param>
    /// <param name="pipe">The pipe applied to each Event Hubs send context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The task returned by the wrapped producer.</returns>
    public Task ProduceAsync<T>(IEnumerable<T> messages, IPipe<EventHubSendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        Used?.Invoke();
        return _producer.ProduceAsync(messages, pipe, cancellationToken);
    }

    /// <summary>Reports cache usage, initializes one message, and produces it.</summary>
    /// <typeparam name="T">The message type to initialize.</typeparam>
    /// <param name="values">The values used to initialize the message.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The task returned by the wrapped producer.</returns>
    public Task ProduceAsync<T>(object values, CancellationToken cancellationToken = default)
        where T : class
    {
        Used?.Invoke();
        return _producer.ProduceAsync<T>(values, cancellationToken);
    }

    /// <summary>Reports cache usage, initializes a batch of messages, and produces them.</summary>
    /// <typeparam name="T">The message type to initialize.</typeparam>
    /// <param name="values">The values used to initialize the messages.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The task returned by the wrapped producer.</returns>
    public Task ProduceAsync<T>(IEnumerable<object> values, CancellationToken cancellationToken = default)
        where T : class
    {
        Used?.Invoke();
        return _producer.ProduceAsync<T>(values, cancellationToken);
    }

    /// <summary>Reports cache usage, initializes one message, configures its send context, and produces it.</summary>
    /// <typeparam name="T">The message type to initialize.</typeparam>
    /// <param name="values">The values used to initialize the message.</param>
    /// <param name="pipe">The pipe that configures the Event Hubs send context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The task returned by the wrapped producer.</returns>
    public Task ProduceAsync<T>(object values, IPipe<EventHubSendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        Used?.Invoke();
        return _producer.ProduceAsync(values, pipe, cancellationToken);
    }

    /// <summary>Reports cache usage, initializes a batch, configures each send context, and produces the messages.</summary>
    /// <typeparam name="T">The message type to initialize.</typeparam>
    /// <param name="values">The values used to initialize the messages.</param>
    /// <param name="pipe">The pipe applied to each Event Hubs send context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The task returned by the wrapped producer.</returns>
    public Task ProduceAsync<T>(IEnumerable<object> values, IPipe<EventHubSendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        Used?.Invoke();
        return _producer.ProduceAsync(values, pipe, cancellationToken);
    }

    /// <summary>Occurs whenever an operation is delegated to the wrapped producer.</summary>
    public event Action? Used;
}
