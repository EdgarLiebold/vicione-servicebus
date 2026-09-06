using System.Collections.Generic;
using System.Threading;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>Deduplicates and links Amazon SNS topic, Amazon SQS queue, and subscription topology entities.</summary>
public abstract class BrokerTopologyBuilder
{
    /// <summary>Stores deduplicated Amazon SQS queue entities.</summary>
    protected readonly NamedEntityCollection<QueueEntity, QueueHandle> Queues;
    /// <summary>Stores deduplicated topic-to-queue subscriptions.</summary>
    protected readonly NamedEntityCollection<QueueSubscriptionEntity, QueueSubscriptionHandle> QueueSubscriptions;
    /// <summary>Stores deduplicated Amazon SNS topic entities.</summary>
    protected readonly NamedEntityCollection<TopicEntity, TopicHandle> Topics;
    long _nextId;

    /// <summary>Initializes empty provider topology collections.</summary>
    protected BrokerTopologyBuilder()
    {
        Topics = new NamedEntityCollection<TopicEntity, TopicHandle>(TopicEntity.EntityComparer, TopicEntity.NameComparer);
        Queues = new NamedEntityCollection<QueueEntity, QueueHandle>(QueueEntity.QueueComparer, QueueEntity.NameComparer);
        QueueSubscriptions =
            new NamedEntityCollection<QueueSubscriptionEntity, QueueSubscriptionHandle>(QueueSubscriptionEntity.EntityComparer,
                QueueSubscriptionEntity.NameComparer);
    }

    long GetNextId()
    {
        return Interlocked.Increment(ref _nextId);
    }

    /// <summary>Adds or reuses an Amazon SNS topic entity.</summary>
    /// <param name="name">The topic name.</param>
    /// <param name="durable">Whether the topic is retained when its endpoint stops.</param>
    /// <param name="autoDelete">Whether the topic is deleted when its endpoint stops.</param>
    /// <param name="topicAttributes">Optional Amazon SNS topic attributes.</param>
    /// <param name="topicSubscriptionAttributes">Optional default subscription attributes.</param>
    /// <param name="tags">Optional topic tags.</param>
    /// <returns>A handle to the deduplicated topic entity.</returns>
    public TopicHandle CreateTopic(string name, bool durable, bool autoDelete, IDictionary<string, object>? topicAttributes = null,
        IDictionary<string, object>? topicSubscriptionAttributes = null, IDictionary<string, string>? tags = null)
    {
        var id = GetNextId();

        var topicEntity = new TopicEntity(id, name, durable, autoDelete, topicAttributes, topicSubscriptionAttributes, tags);

        return Topics.GetOrAdd(topicEntity);
    }

    /// <summary>Adds or reuses an Amazon SQS queue entity.</summary>
    /// <param name="name">The queue name.</param>
    /// <param name="durable">Whether the queue is retained when its endpoint stops.</param>
    /// <param name="autoDelete">Whether the queue is deleted when its endpoint stops.</param>
    /// <param name="queueAttributes">Optional Amazon SQS queue attributes.</param>
    /// <param name="queueSubscriptionAttributes">Optional attributes for subscriptions targeting the queue.</param>
    /// <param name="tags">Optional queue tags.</param>
    /// <returns>A handle to the deduplicated queue entity.</returns>
    public QueueHandle CreateQueue(string name, bool durable, bool autoDelete, IDictionary<string, object>? queueAttributes = null,
        IDictionary<string, object>? queueSubscriptionAttributes = null, IDictionary<string, string>? tags = null)
    {
        var id = GetNextId();

        var queueEntity = new QueueEntity(id, name, durable, autoDelete, queueAttributes, queueSubscriptionAttributes, tags);

        return Queues.GetOrAdd(queueEntity);
    }

    /// <summary>Adds or reuses a subscription from an Amazon SNS topic to an Amazon SQS queue.</summary>
    /// <param name="topic">The source topic handle.</param>
    /// <param name="queue">The destination queue handle.</param>
    /// <returns>A handle to the deduplicated subscription.</returns>
    public QueueSubscriptionHandle CreateQueueSubscription(TopicHandle topic, QueueHandle queue)
    {
        var id = GetNextId();

        var topicEntity = Topics.Get(topic);

        var queueEntity = Queues.Get(queue);

        var binding = new QueueSubscriptionEntity(id, topicEntity, queueEntity);

        return QueueSubscriptions.GetOrAdd(binding);
    }

}
