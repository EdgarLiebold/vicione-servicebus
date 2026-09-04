using System.Collections.Generic;
using System.Threading;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>
/// Provides a broker topology builder implementation.
/// </summary>
public abstract class BrokerTopologyBuilder
{
    /// <summary>
    /// Defines the queues value.
    /// </summary>
    protected readonly NamedEntityCollection<QueueEntity, QueueHandle> Queues;
    /// <summary>
    /// Defines the queue subscriptions value.
    /// </summary>
    protected readonly NamedEntityCollection<QueueSubscriptionEntity, QueueSubscriptionHandle> QueueSubscriptions;
    /// <summary>
    /// Defines the topics value.
    /// </summary>
    protected readonly NamedEntityCollection<TopicEntity, TopicHandle> Topics;
    /// <summary>
    /// Defines the topic subscriptions value.
    /// </summary>
    protected readonly NamedEntityCollection<TopicSubscriptionEntity, TopicSubscriptionHandle> TopicSubscriptions;
    long _nextId;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    protected BrokerTopologyBuilder()
    {
        Topics = new NamedEntityCollection<TopicEntity, TopicHandle>(TopicEntity.EntityComparer, TopicEntity.NameComparer);
        Queues = new NamedEntityCollection<QueueEntity, QueueHandle>(QueueEntity.QueueComparer, QueueEntity.NameComparer);
        QueueSubscriptions =
            new NamedEntityCollection<QueueSubscriptionEntity, QueueSubscriptionHandle>(QueueSubscriptionEntity.EntityComparer,
                QueueSubscriptionEntity.NameComparer);
        TopicSubscriptions =
            new NamedEntityCollection<TopicSubscriptionEntity, TopicSubscriptionHandle>(TopicSubscriptionEntity.EntityComparer,
                TopicSubscriptionEntity.NameComparer);
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
    /// <param name="topicAttributes">The topic attributes value.</param>
    /// <param name="topicSubscriptionAttributes">The topic subscription attributes value.</param>
    /// <param name="tags">The tags value.</param>
    /// <returns>The result of the operation.</returns>
    public TopicHandle CreateTopic(string name, bool durable, bool autoDelete, IDictionary<string, object>? topicAttributes = null,
        IDictionary<string, object>? topicSubscriptionAttributes = null, IDictionary<string, string>? tags = null)
    {
        var id = GetNextId();

        var topicEntity = new TopicEntity(id, name, durable, autoDelete, topicAttributes, topicSubscriptionAttributes, tags);

        return Topics.GetOrAdd(topicEntity);
    }

    /// <summary>
    /// Creates queue.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="durable">The durable value.</param>
    /// <param name="autoDelete">The auto delete value.</param>
    /// <param name="queueAttributes">The queue attributes value.</param>
    /// <param name="queueSubscriptionAttributes">The queue subscription attributes value.</param>
    /// <param name="tags">The tags value.</param>
    /// <returns>The result of the operation.</returns>
    public QueueHandle CreateQueue(string name, bool durable, bool autoDelete, IDictionary<string, object>? queueAttributes = null,
        IDictionary<string, object>? queueSubscriptionAttributes = null, IDictionary<string, string>? tags = null)
    {
        var id = GetNextId();

        var queueEntity = new QueueEntity(id, name, durable, autoDelete, queueAttributes, queueSubscriptionAttributes, tags);

        return Queues.GetOrAdd(queueEntity);
    }

    /// <summary>
    /// Creates queue subscription.
    /// </summary>
    /// <param name="topic">The topic value.</param>
    /// <param name="queue">The queue value.</param>
    /// <returns>The result of the operation.</returns>
    public QueueSubscriptionHandle CreateQueueSubscription(TopicHandle topic, QueueHandle queue)
    {
        var id = GetNextId();

        var topicEntity = Topics.Get(topic);

        var queueEntity = Queues.Get(queue);

        var binding = new QueueSubscriptionEntity(id, topicEntity, queueEntity);

        return QueueSubscriptions.GetOrAdd(binding);
    }

    /// <summary>
    /// Creates topic subscription.
    /// </summary>
    /// <param name="source">The source value.</param>
    /// <param name="destination">The destination value.</param>
    /// <returns>The result of the operation.</returns>
    public TopicSubscriptionHandle CreateTopicSubscription(TopicHandle source, TopicHandle destination)
    {
        var id = GetNextId();

        var sourceEntity = Topics.Get(source);

        var destinationEntity = Topics.Get(destination);

        var binding = new TopicSubscriptionEntity(id, sourceEntity, destinationEntity);

        return TopicSubscriptions.GetOrAdd(binding);
    }
}
