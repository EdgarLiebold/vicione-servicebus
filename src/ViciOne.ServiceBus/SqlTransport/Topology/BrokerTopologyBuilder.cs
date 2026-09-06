using System;
using System.Threading;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>
/// Provides a broker topology builder implementation.
/// </summary>
public abstract class BrokerTopologyBuilder
{
    readonly NamedEntityCollection<QueueEntity, QueueHandle> _queues;
    readonly EntityCollection<QueueSubscriptionEntity, QueueSubscriptionHandle> _queueSubscriptions;
    readonly NamedEntityCollection<TopicEntity, TopicHandle> _topics;
    readonly EntityCollection<TopicSubscriptionEntity, TopicSubscriptionHandle> _topicSubscriptions;
    long _nextId;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    protected BrokerTopologyBuilder()
    {
        _topics = new NamedEntityCollection<TopicEntity, TopicHandle>(TopicEntity.EntityComparer, TopicEntity.NameComparer);
        _queues = new NamedEntityCollection<QueueEntity, QueueHandle>(QueueEntity.QueueComparer, QueueEntity.NameComparer);

        _topicSubscriptions = new EntityCollection<TopicSubscriptionEntity, TopicSubscriptionHandle>(TopicSubscriptionEntity.EntityComparer);
        _queueSubscriptions = new EntityCollection<QueueSubscriptionEntity, QueueSubscriptionHandle>(QueueSubscriptionEntity.EntityComparer);
    }

    long GetNextId()
    {
        return Interlocked.Increment(ref _nextId);
    }

    /// <summary>
    /// Creates topic.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns>The result of the operation.</returns>
    public TopicHandle CreateTopic(string name)
    {
        var id = GetNextId();

        var exchange = new TopicEntity(id, name);

        return _topics.GetOrAdd(exchange);
    }

    /// <summary>
    /// Creates topic subscription.
    /// </summary>
    /// <param name="source">The source value.</param>
    /// <param name="destination">The destination value.</param>
    /// <param name="subscriptionType">The subscription type value.</param>
    /// <param name="routingKey">The routing key value.</param>
    /// <returns>The result of the operation.</returns>
    public TopicSubscriptionHandle CreateTopicSubscription(TopicHandle source, TopicHandle destination, SqlSubscriptionType subscriptionType,
        string? routingKey)
    {
        var id = GetNextId();

        var sourceExchange = _topics.Get(source);

        var destinationExchange = _topics.Get(destination);

        var binding = new TopicSubscriptionEntity(id, sourceExchange, destinationExchange, subscriptionType, routingKey);

        return _topicSubscriptions.GetOrAdd(binding);
    }

    /// <summary>
    /// Creates queue.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="autoDeleteOnIdle">The auto delete on idle value.</param>
    /// <param name="maxDeliveryCount">The max delivery count value.</param>
    /// <returns>The result of the operation.</returns>
    public QueueHandle CreateQueue(string name, TimeSpan? autoDeleteOnIdle = null, int? maxDeliveryCount = null)
    {
        var id = GetNextId();

        var queue = new QueueEntity(id, name, autoDeleteOnIdle, maxDeliveryCount);

        return _queues.GetOrAdd(queue);
    }

    /// <summary>
    /// Creates queue subscription.
    /// </summary>
    /// <param name="topic">The topic value.</param>
    /// <param name="queue">The queue value.</param>
    /// <param name="subscriptionType">The subscription type value.</param>
    /// <param name="routingKey">The routing key value.</param>
    /// <returns>The result of the operation.</returns>
    public QueueSubscriptionHandle CreateQueueSubscription(TopicHandle topic, QueueHandle queue, SqlSubscriptionType subscriptionType, string? routingKey)
    {
        var id = GetNextId();

        var exchangeEntity = _topics.Get(topic);

        var queueEntity = _queues.Get(queue);

        var binding = new QueueSubscriptionEntity(id, exchangeEntity, queueEntity, subscriptionType, routingKey);

        return _queueSubscriptions.GetOrAdd(binding);
    }

    /// <summary>
    /// Performs the build broker topology operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public BrokerTopology BuildBrokerTopology()
    {
        return new SqlBrokerTopology(_topics, _topicSubscriptions, _queues, _queueSubscriptions);
    }
}
