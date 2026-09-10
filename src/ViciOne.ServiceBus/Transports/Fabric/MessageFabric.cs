using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals.GraphValidation;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Providers.Transports;

namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>Provides the in-process message fabric for message.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
/// <typeparam name="T">The value type.</typeparam>
public class MessageFabric<TContext, T> :
    Supervisor,
    IMessageFabric<TContext, T>
    where T : class
    where TContext : class
{
    readonly InMemoryDelayProvider _delayProvider;
    readonly ConcurrentDictionary<string, IMessageExchange<T>> _exchanges;
    readonly MessageFabricObservable<TContext> _observers;
    readonly int _queueCapacity;
    readonly ConcurrentDictionary<string, IMessageQueue<TContext, T>> _queues;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="queueCapacity">The queue capacity.</param>
    public MessageFabric(int queueCapacity = 1024)
    {
        if (queueCapacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(queueCapacity), queueCapacity, "Queue capacity must be greater than zero.");

        _queueCapacity = queueCapacity;
        _observers = new MessageFabricObservable<TContext>();
        _delayProvider = new InMemoryDelayProvider();

        _exchanges = new ConcurrentDictionary<string, IMessageExchange<T>>(StringComparer.OrdinalIgnoreCase);
        _queues = new ConcurrentDictionary<string, IMessageQueue<TContext, T>>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Gets the delay provider.</summary>
    public IInMemoryDelayProvider DelayProvider => _delayProvider;

    /// <summary>Declares the configured exchange.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="name">The name.</param>
    /// <param name="exchangeType">The runtime exchange type used by the operation.</param>
    public void ExchangeDeclare(TContext context, string name, ExchangeType exchangeType)
    {
        GetOrAddExchange(context, name, exchangeType);
    }

    /// <summary>Binds the configured exchange.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="source">The source value.</param>
    /// <param name="destination">The destination.</param>
    /// <param name="routingKey">The routing key.</param>
    public void ExchangeBind(TContext context, string source, string destination, string? routingKey)
    {
        if (source.Equals(destination))
            throw new ArgumentException("The source and destination exchange cannot be the same: " + source);

        IMessageExchange<T> sourceExchange = GetOrAddExchange(context, source, ExchangeType.FanOut);

        IMessageExchange<T> destinationExchange = GetOrAddExchange(context, destination, ExchangeType.FanOut);

        ValidateBinding(destinationExchange, sourceExchange);

        _observers.ExchangeBindingCreated(context, source, destination, routingKey);

        sourceExchange.Connect(destinationExchange, routingKey);
    }

    /// <summary>Declares the configured queue.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="name">The name.</param>
    public void QueueDeclare(TContext context, string name)
    {
        GetOrAddQueue(context, name);
    }

    /// <summary>Binds the configured queue.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="source">The source value.</param>
    /// <param name="destination">The destination.</param>
    public void QueueBind(TContext context, string source, string destination)
    {
        IMessageExchange<T> sourceExchange = GetOrAddExchange(context, source, ExchangeType.FanOut);

        IMessageQueue<TContext, T> destinationQueue = GetOrAddQueue(context, destination);

        ValidateBinding(destinationQueue, sourceExchange);

        _observers.QueueBindingCreated(context, source, destination);

        sourceExchange.Connect(destinationQueue, null);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("messageFabric");
        foreach (IMessageExchange<T> exchange in _exchanges.Values)
            exchange.Probe(scope);

        foreach (IMessageQueue<TContext, T> queue in _queues.Values)
            queue.Probe(scope);
    }

    /// <summary>Gets exchange.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="name">The name.</param>
    /// <param name="exchangeType">The runtime exchange type used by the operation.</param>
    /// <returns>The exchange.</returns>
    public IMessageExchange<T> GetExchange(TContext context, string name, ExchangeType exchangeType)
    {
        return GetOrAddExchange(context, name, exchangeType);
    }

    /// <summary>Gets queue.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="name">The name.</param>
    /// <returns>The queue.</returns>
    public IMessageQueue<TContext, T> GetQueue(TContext context, string name)
    {
        return GetOrAddQueue(context, name);
    }

    /// <summary>Connects message fabric observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectMessageFabricObserver(IMessageFabricObserver<TContext> observer)
    {
        return _observers.Connect(observer);
    }

    /// <summary>Stops supervisor.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected override async Task StopSupervisorAsync(StopSupervisorContext context)
    {
        await base.StopSupervisorAsync(context).ConfigureAwait(false);

        await _delayProvider.DisposeAsync().ConfigureAwait(false);
    }

    IMessageQueue<TContext, T> GetOrAddQueue(TContext context, string name)
    {
        MessageQueue<TContext, T>? created = null;
        IMessageQueue<TContext, T> queue = _queues.GetOrAdd(name, x =>
        {
            created = new MessageQueue<TContext, T>(_observers, name, _delayProvider, _queueCapacity);

            return created;
        });

        if (created != null && queue == created)
        {
            Add(created);

            _observers.QueueDeclared(context, name);
        }

        return queue;
    }

    IMessageExchange<T> GetOrAddExchange(TContext context, string name, ExchangeType exchangeType)
    {
        IMessageExchange<T>? created = null;
        IMessageExchange<T> exchange = _exchanges.GetOrAdd(name, x =>
        {
            created = exchangeType switch
            {
                ExchangeType.FanOut => new MessageFanOutExchange<T>(name),
                ExchangeType.Direct => new MessageDirectExchange<T>(name),
                ExchangeType.Topic => new MessageTopicExchange<T>(name),
                _ => throw new ArgumentException($"Unsupported exchange type: {exchangeType}", nameof(exchangeType))
            };

            return created;
        });

        if (created != null && exchange == created)
            _observers.ExchangeDeclared(context, name, exchangeType);

        return exchange;
    }

    void ValidateBinding(IMessageSink<T> destination, IMessageSink<T> sourceExchange)
    {
        try
        {
            var graph = new DependencyGraph<IMessageSink<T>>(_exchanges.Count + 1);
            var exchanges = new List<IMessageExchange<T>>(_exchanges.Values);
            foreach (IMessageExchange<T> exchange in exchanges)
            {
                var sinks = new List<IMessageSink<T>>(exchange.Sinks);
                foreach (IMessageSink<T> sink in sinks)
                    graph.Add(sink, exchange);
            }

            graph.Add(destination, sourceExchange);

            graph.EnsureGraphIsAcyclic();
        }
        catch (CyclicGraphException exception)
        {
            throw new InvalidOperationException("The exchange binding would create a cycle in the messaging fabric.", exception);
        }
    }
}
