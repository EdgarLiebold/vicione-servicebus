using System;
using System.Threading;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>Builds broker topology components.</summary>
public abstract class BrokerTopologyBuilder
{
    readonly NamedEntityCollection<QueueEntity, QueueHandle> _queues;
    readonly EntityCollection<QueueSubscriptionEntity, QueueSubscriptionHandle> _queueSubscriptions;
    readonly NamedEntityCollection<TopicEntity, TopicHandle> _topics;
    readonly EntityCollection<TopicSubscriptionEntity, TopicSubscriptionHandle> _topicSubscriptions;
    long _nextId;

    /// <summary>Initializes a new instance.</summary>
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

    /// <summary>Creates topic.</summary>
    /// <param name="name">The name.</param>
    /// <returns>The created topic.</returns>
    public TopicHandle CreateTopic(string name)
    {
        var id = GetNextId();

        var exchange = new TopicEntity(id, name);

        return _topics.GetOrAdd(exchange);
    }

    /// <summary>Creates topic subscription.</summary>
    /// <param name="source">The source value.</param>
    /// <param name="destination">The destination.</param>
    /// <param name="subscriptionType">The runtime subscription type used by the operation.</param>
    /// <param name="routingKey">The routing key.</param>
    /// <returns>The created topic subscription.</returns>
    public TopicSubscriptionHandle CreateTopicSubscription(TopicHandle source, TopicHandle destination, SqlSubscriptionType subscriptionType,
        string? routingKey)
    {
        var id = GetNextId();

        var sourceExchange = _topics.Get(source);

        var destinationExchange = _topics.Get(destination);

        var binding = new TopicSubscriptionEntity(id, sourceExchange, destinationExchange, subscriptionType, routingKey);

        return _topicSubscriptions.GetOrAdd(binding);
    }

    /// <summary>Creates queue.</summary>
    /// <param name="name">The name.</param>
    /// <param name="autoDeleteOnIdle">The auto delete on idle.</param>
    /// <param name="maxDeliveryCount">The max delivery count.</param>
    /// <returns>The created queue.</returns>
    public QueueHandle CreateQueue(string name, TimeSpan? autoDeleteOnIdle = null, int? maxDeliveryCount = null)
    {
        var id = GetNextId();

        var queue = new QueueEntity(id, name, autoDeleteOnIdle, maxDeliveryCount);

        return _queues.GetOrAdd(queue);
    }

    /// <summary>Creates queue subscription.</summary>
    /// <param name="topic">The topic.</param>
    /// <param name="queue">The queue.</param>
    /// <param name="subscriptionType">The runtime subscription type used by the operation.</param>
    /// <param name="routingKey">The routing key.</param>
    /// <returns>The created queue subscription.</returns>
    public QueueSubscriptionHandle CreateQueueSubscription(TopicHandle topic, QueueHandle queue, SqlSubscriptionType subscriptionType, string? routingKey)
    {
        var id = GetNextId();

        var exchangeEntity = _topics.Get(topic);

        var queueEntity = _queues.Get(queue);

        var binding = new QueueSubscriptionEntity(id, exchangeEntity, queueEntity, subscriptionType, routingKey);

        return _queueSubscriptions.GetOrAdd(binding);
    }

    /// <summary>Builds broker topology.</summary>
    /// <returns>The configured broker topology.</returns>
    public BrokerTopology BuildBrokerTopology()
    {
        return new SqlBrokerTopology(_topics, _topicSubscriptions, _queues, _queueSubscriptions);
    }
}
