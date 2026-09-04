using System.Threading;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>
/// Provides a broker topology builder implementation.
/// </summary>
public abstract class BrokerTopologyBuilder
{
    /// <summary>
    /// Defines the consumers value.
    /// </summary>
    protected readonly NamedEntityCollection<ConsumerEntity, ConsumerHandle> Consumers;
    /// <summary>
    /// Defines the queues value.
    /// </summary>
    protected readonly NamedEntityCollection<QueueEntity, QueueHandle> Queues;
    /// <summary>
    /// Defines the topics value.
    /// </summary>
    protected readonly NamedEntityCollection<TopicEntity, TopicHandle> Topics;
    long _nextId;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
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

    /// <summary>
    /// Creates topic.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="durable">The durable value.</param>
    /// <param name="autoDelete">The auto delete value.</param>
    /// <returns>The result of the operation.</returns>
    public TopicHandle CreateTopic(string name, bool durable, bool autoDelete)
    {
        var id = GetNextId();

        var exchange = new TopicEntity(id, name, durable, autoDelete);

        return Topics.GetOrAdd(exchange);
    }

    /// <summary>
    /// Creates queue.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="durable">The durable value.</param>
    /// <param name="autoDelete">The auto delete value.</param>
    /// <returns>The result of the operation.</returns>
    public QueueHandle CreateQueue(string name, bool durable, bool autoDelete)
    {
        var id = GetNextId();

        var queue = new QueueEntity(id, name, durable, autoDelete);

        return Queues.GetOrAdd(queue);
    }

    /// <summary>
    /// Performs the bind consumer operation.
    /// </summary>
    /// <param name="topic">The topic value.</param>
    /// <param name="queue">The queue value.</param>
    /// <param name="selector">The selector value.</param>
    /// <param name="consumerName">The consumer name value.</param>
    /// <param name="shared">The shared value.</param>
    /// <returns>The result of the operation.</returns>
    public ConsumerHandle BindConsumer(TopicHandle topic, QueueHandle? queue, string? selector, string? consumerName = null, bool shared = false)
    {
        var id = GetNextId();

        var exchangeEntity = Topics.Get(topic);

        var queueEntity = queue != null ? Queues.Get(queue) : null;

        var binding = new ConsumerEntity(id, exchangeEntity, queueEntity, selector, consumerName, shared);

        return Consumers.GetOrAdd(binding);
    }
}
