using System.Collections.Generic;
using System.Threading;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>Collects and de-duplicates RabbitMQ exchange, queue, and binding declarations.</summary>
public abstract class BrokerTopologyBuilder
{
    readonly EntityCollection<ExchangeBindingEntity, ExchangeBindingHandle> _exchangeBindings;
    readonly NamedEntityCollection<ExchangeEntity, ExchangeHandle> _exchanges;
    readonly EntityCollection<QueueBindingEntity, QueueBindingHandle> _queueBindings;
    readonly NamedEntityCollection<QueueEntity, QueueHandle> _queues;
    long _nextId;

    /// <summary>Creates an empty topology builder.</summary>
    protected BrokerTopologyBuilder()
    {
        _exchanges = new NamedEntityCollection<ExchangeEntity, ExchangeHandle>(ExchangeEntity.EntityComparer, ExchangeEntity.NameComparer);
        _queues = new NamedEntityCollection<QueueEntity, QueueHandle>(QueueEntity.QueueComparer, QueueEntity.NameComparer);

        _exchangeBindings = new EntityCollection<ExchangeBindingEntity, ExchangeBindingHandle>(ExchangeBindingEntity.EntityComparer);
        _queueBindings = new EntityCollection<QueueBindingEntity, QueueBindingHandle>(QueueBindingEntity.EntityComparer);
    }

    long GetNextId()
    {
        return Interlocked.Increment(ref _nextId);
    }

    /// <summary>Adds or reuses an exchange declaration.</summary>
    /// <param name="name">The exchange name.</param>
    /// <param name="type">The RabbitMQ exchange type.</param>
    /// <param name="durable">Whether the exchange survives broker restarts.</param>
    /// <param name="autoDelete">Whether RabbitMQ deletes the exchange when it is no longer used.</param>
    /// <param name="arguments">The broker-specific declaration arguments.</param>
    /// <returns>A handle that identifies the de-duplicated exchange.</returns>
    public ExchangeHandle ExchangeDeclare(string name, string type, bool durable, bool autoDelete, IDictionary<string, object?> arguments)
    {
        var id = GetNextId();

        var exchange = new ExchangeEntity(id, name, type, durable, autoDelete, arguments);

        return _exchanges.GetOrAdd(exchange);
    }

    /// <summary>Adds or reuses a binding between two declared exchanges.</summary>
    /// <param name="source">The source exchange handle.</param>
    /// <param name="destination">The destination exchange handle.</param>
    /// <param name="routingKey">The routing key used by the binding.</param>
    /// <param name="arguments">The broker-specific binding arguments.</param>
    /// <returns>A handle that identifies the de-duplicated binding.</returns>
    public ExchangeBindingHandle ExchangeBind(ExchangeHandle source, ExchangeHandle destination, string routingKey, IDictionary<string, object?> arguments)
    {
        var id = GetNextId();

        var sourceExchange = _exchanges.Get(source);

        var destinationExchange = _exchanges.Get(destination);

        var binding = new ExchangeBindingEntity(id, sourceExchange, destinationExchange, routingKey, arguments);

        return _exchangeBindings.GetOrAdd(binding);
    }

    /// <summary>Adds or reuses a queue declaration, normalizing quorum and expiring-queue constraints.</summary>
    /// <param name="name">The queue name.</param>
    /// <param name="durable">Whether the queue survives broker restarts.</param>
    /// <param name="autoDelete">Whether RabbitMQ deletes the queue when its last consumer is gone.</param>
    /// <param name="exclusive">Whether the queue belongs exclusively to its declaring connection.</param>
    /// <param name="arguments">The broker-specific declaration arguments.</param>
    /// <returns>A handle that identifies the de-duplicated queue.</returns>
    public QueueHandle QueueDeclare(string name, bool durable, bool autoDelete, bool exclusive, IDictionary<string, object?> arguments)
    {
        var id = GetNextId();

        var queueArguments = new Dictionary<string, object?>(arguments);

        var queueAutoDelete = autoDelete;
        if (queueArguments.TryGetValue(RabbitMQ.Client.Headers.XExpires, out _))
        {
            queueAutoDelete = false;
            autoDelete = true;
        }

        var isQuorumQueue = queueArguments.TryGetValue(RabbitMQ.Client.Headers.XQueueType, out var queueType) && Equals(queueType, "quorum");

        var durableQueue = durable || isQuorumQueue;

        exclusive = exclusive || autoDelete && !durableQueue;

        var queue = new QueueEntity(id, name, durableQueue, queueAutoDelete, exclusive, arguments);

        return _queues.GetOrAdd(queue);
    }

    /// <summary>Adds or reuses a binding from a declared exchange to a declared queue.</summary>
    /// <param name="exchange">The source exchange handle.</param>
    /// <param name="queue">The destination queue handle.</param>
    /// <param name="routingKey">The routing key used by the binding.</param>
    /// <param name="arguments">The broker-specific binding arguments.</param>
    /// <returns>A handle that identifies the de-duplicated binding.</returns>
    public QueueBindingHandle QueueBind(ExchangeHandle exchange, QueueHandle queue, string routingKey, IDictionary<string, object?> arguments)
    {
        var id = GetNextId();

        var exchangeEntity = _exchanges.Get(exchange);

        var queueEntity = _queues.Get(queue);

        var binding = new QueueBindingEntity(id, exchangeEntity, queueEntity, routingKey, arguments);

        return _queueBindings.GetOrAdd(binding);
    }

    /// <summary>Creates an immutable broker-topology snapshot from the collected declarations.</summary>
    /// <returns>The ordered, de-duplicated RabbitMQ topology.</returns>
    public BrokerTopology BuildBrokerTopology()
    {
        return new RabbitMqBrokerTopology(_exchanges, _exchangeBindings, _queues, _queueBindings);
    }
}
