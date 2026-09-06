using System.Threading;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>Builds de-duplicated ActiveMQ topic, queue, and consumer entities.</summary>
public abstract class BrokerTopologyBuilder
{
    /// <summary>The consumer bindings indexed by identity and destination name.</summary>
    protected readonly NamedEntityCollection<ConsumerEntity, ConsumerHandle> Consumers;
    /// <summary>The queues indexed by identity and entity name.</summary>
    protected readonly NamedEntityCollection<QueueEntity, QueueHandle> Queues;
    /// <summary>The topics indexed by identity and entity name.</summary>
    protected readonly NamedEntityCollection<TopicEntity, TopicHandle> Topics;
    long _nextId;

    /// <summary>Creates empty named collections for broker entities.</summary>
    protected BrokerTopologyBuilder()
    {
        Topics = new NamedEntityCollection<TopicEntity, TopicHandle>(TopicEntity.EntityComparer, TopicEntity.NameComparer);
        Queues = new NamedEntityCollection<QueueEntity, QueueHandle>(QueueEntity.QueueComparer, QueueEntity.NameComparer);

        Consumers = new NamedEntityCollection<ConsumerEntity, ConsumerHandle>(ConsumerEntity.EntityComparer, ConsumerEntity.NameComparer);
    }

    long GetNextId()
    {
        return Interlocked.Increment(ref _nextId);
    }

    /// <summary>Gets or adds a topic with the specified lifecycle settings.</summary>
    /// <param name="name">The topic name.</param>
    /// <param name="durable">Whether the topic persists across broker restarts.</param>
    /// <param name="autoDelete">Whether the broker removes the topic when it is no longer used.</param>
    /// <returns>A handle for the de-duplicated topic.</returns>
    public TopicHandle CreateTopic(string name, bool durable, bool autoDelete)
    {
        var id = GetNextId();

        var exchange = new TopicEntity(id, name, durable, autoDelete);

        return Topics.GetOrAdd(exchange);
    }

    /// <summary>Gets or adds a queue with the specified lifecycle settings.</summary>
    /// <param name="name">The queue name.</param>
    /// <param name="durable">Whether the queue persists across broker restarts.</param>
    /// <param name="autoDelete">Whether the broker removes the queue when it is no longer used.</param>
    /// <returns>A handle for the de-duplicated queue.</returns>
    public QueueHandle CreateQueue(string name, bool durable, bool autoDelete)
    {
        var id = GetNextId();

        var queue = new QueueEntity(id, name, durable, autoDelete);

        return Queues.GetOrAdd(queue);
    }

    /// <summary>Gets or adds a consumer binding from a topic to a queue or named topic subscription.</summary>
    /// <param name="topic">The source topic handle.</param>
    /// <param name="queue">The destination queue, or <see langword="null" /> for a direct topic consumer.</param>
    /// <param name="selector">An optional Apache NMS message selector.</param>
    /// <param name="consumerName">An optional native subscription name.</param>
    /// <param name="shared">Whether the named topic subscription is shared.</param>
    /// <returns>A handle for the de-duplicated consumer binding.</returns>
    public ConsumerHandle BindConsumer(TopicHandle topic, QueueHandle? queue, string? selector, string? consumerName = null, bool shared = false)
    {
        var id = GetNextId();

        var exchangeEntity = Topics.Get(topic);

        var queueEntity = queue != null ? Queues.Get(queue) : null;

        var binding = new ConsumerEntity(id, exchangeEntity, queueEntity, selector, consumerName, shared);

        return Consumers.GetOrAdd(binding);
    }
}
