using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Caching;

namespace ViciOne.ServiceBus.EventHubIntegration;

public class CachedEventHubProducer<TKey> :
    IEventHubProducer,
    IResourceUsageSource,
    IAsyncDisposable
    where TKey : notnull
{
    readonly IEventHubProducer _producer;

    public CachedEventHubProducer(TKey key, IEventHubProducer producer)
    {
        Key = key;
        _producer = producer;
    }

    public TKey Key { get; }

    public ValueTask DisposeAsync()
    {
        return _producer switch
        {
            IAsyncDisposable disposable => disposable.DisposeAsync(),
            _ => default
        };
    }

    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        Used?.Invoke();
        return _producer.ConnectSendObserver(observer);
    }

    public Task ProduceAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class
    {
        Used?.Invoke();
        return _producer.ProduceAsync(message, cancellationToken);
    }

    public Task ProduceAsync<T>(IEnumerable<T> messages, CancellationToken cancellationToken = default)
        where T : class
    {
        Used?.Invoke();
        return _producer.ProduceAsync(messages, cancellationToken);
    }

    public Task ProduceAsync<T>(T message, IPipe<EventHubSendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        Used?.Invoke();
        return _producer.ProduceAsync(message, pipe, cancellationToken);
    }

    public Task ProduceAsync<T>(IEnumerable<T> messages, IPipe<EventHubSendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        Used?.Invoke();
        return _producer.ProduceAsync(messages, pipe, cancellationToken);
    }

    public Task ProduceAsync<T>(object values, CancellationToken cancellationToken = default)
        where T : class
    {
        Used?.Invoke();
        return _producer.ProduceAsync<T>(values, cancellationToken);
    }

    public Task ProduceAsync<T>(IEnumerable<object> values, CancellationToken cancellationToken = default)
        where T : class
    {
        Used?.Invoke();
        return _producer.ProduceAsync<T>(values, cancellationToken);
    }

    public Task ProduceAsync<T>(object values, IPipe<EventHubSendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        Used?.Invoke();
        return _producer.ProduceAsync(values, pipe, cancellationToken);
    }

    public Task ProduceAsync<T>(IEnumerable<object> values, IPipe<EventHubSendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        Used?.Invoke();
        return _producer.ProduceAsync(values, pipe, cancellationToken);
    }

    public event Action? Used;
}
