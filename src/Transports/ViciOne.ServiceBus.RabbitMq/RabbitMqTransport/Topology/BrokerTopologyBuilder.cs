using System.Collections.Generic;
using System.Threading;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>
/// Provides a broker topology builder implementation.
/// </summary>
public abstract class BrokerTopologyBuilder
{
    readonly EntityCollection<ExchangeBindingEntity, ExchangeBindingHandle> _exchangeBindings;
    readonly NamedEntityCollection<ExchangeEntity, ExchangeHandle> _exchanges;
    readonly EntityCollection<QueueBindingEntity, QueueBindingHandle> _queueBindings;
    readonly NamedEntityCollection<QueueEntity, QueueHandle> _queues;
    long _nextId;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
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

    /// <summary>
    /// Performs the exchange declare operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="type">The type value.</param>
    /// <param name="durable">The durable value.</param>
    /// <param name="autoDelete">The auto delete value.</param>
    /// <param name="arguments">The arguments value.</param>
    /// <returns>The result of the operation.</returns>
    public ExchangeHandle ExchangeDeclare(string name, string type, bool durable, bool autoDelete, IDictionary<string, object?> arguments)
    {
        var id = GetNextId();

        var exchange = new ExchangeEntity(id, name, type, durable, autoDelete, arguments);

        return _exchanges.GetOrAdd(exchange);
    }

    /// <summary>
    /// Performs the exchange bind operation.
    /// </summary>
    /// <param name="source">The source value.</param>
    /// <param name="destination">The destination value.</param>
    /// <param name="routingKey">The routing key value.</param>
    /// <param name="arguments">The arguments value.</param>
    /// <returns>The result of the operation.</returns>
    public ExchangeBindingHandle ExchangeBind(ExchangeHandle source, ExchangeHandle destination, string routingKey, IDictionary<string, object?> arguments)
    {
        var id = GetNextId();

        var sourceExchange = _exchanges.Get(source);

        var destinationExchange = _exchanges.Get(destination);

        var binding = new ExchangeBindingEntity(id, sourceExchange, destinationExchange, routingKey, arguments);

        return _exchangeBindings.GetOrAdd(binding);
    }

    /// <summary>
    /// Performs the queue declare operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="durable">The durable value.</param>
    /// <param name="autoDelete">The auto delete value.</param>
    /// <param name="exclusive">The exclusive value.</param>
    /// <param name="arguments">The arguments value.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the queue bind operation.
    /// </summary>
    /// <param name="exchange">The exchange value.</param>
    /// <param name="queue">The queue value.</param>
    /// <param name="routingKey">The routing key value.</param>
    /// <param name="arguments">The arguments value.</param>
    /// <returns>The result of the operation.</returns>
    public QueueBindingHandle QueueBind(ExchangeHandle exchange, QueueHandle queue, string routingKey, IDictionary<string, object?> arguments)
    {
        var id = GetNextId();

        var exchangeEntity = _exchanges.Get(exchange);

        var queueEntity = _queues.Get(queue);

        var binding = new QueueBindingEntity(id, exchangeEntity, queueEntity, routingKey, arguments);

        return _queueBindings.GetOrAdd(binding);
    }

    /// <summary>
    /// Performs the build broker topology operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public BrokerTopology BuildBrokerTopology()
    {
        return new RabbitMqBrokerTopology(_exchanges, _exchangeBindings, _queues, _queueBindings);
    }
}
