using System.Threading;
using Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>Collects deduplicated Azure Service Bus entity declarations and subscriptions.</summary>
public class BrokerTopologyBuilder :
    IBrokerTopologyBuilder
{
    long _nextId;

    /// <summary>Creates empty entity collections keyed by broker identity and name.</summary>
    public BrokerTopologyBuilder()
    {
        Topics = new NamedEntityCollection<TopicEntity, TopicHandle>(TopicEntity.EntityComparer, TopicEntity.NameComparer);
        Queues = new NamedEntityCollection<QueueEntity, QueueHandle>(QueueEntity.EntityComparer, QueueEntity.NameComparer);

        Subscriptions = new NamedEntityCollection<SubscriptionEntity, SubscriptionHandle>(SubscriptionEntity.EntityComparer,
            SubscriptionEntity.NameComparer);

        QueueSubscriptions = new NamedEntityCollection<QueueSubscriptionEntity, QueueSubscriptionHandle>(QueueSubscriptionEntity.EntityComparer,
            QueueSubscriptionEntity.NameComparer);

        TopicSubscriptions = new NamedEntityCollection<TopicSubscriptionEntity, TopicSubscriptionHandle>(TopicSubscriptionEntity.EntityComparer,
            TopicSubscriptionEntity.NameComparer);
    }

    /// <summary>Gets topic-to-consumer subscription declarations.</summary>
    protected EntityCollection<SubscriptionEntity, SubscriptionHandle> Subscriptions { get; }
    /// <summary>Gets topic declarations keyed by entity name.</summary>
    protected NamedEntityCollection<TopicEntity, TopicHandle> Topics { get; }
    /// <summary>Gets topic-to-queue forwarding subscriptions.</summary>
    protected EntityCollection<QueueSubscriptionEntity, QueueSubscriptionHandle> QueueSubscriptions { get; }
    /// <summary>Gets topic-to-topic forwarding subscriptions.</summary>
    protected EntityCollection<TopicSubscriptionEntity, TopicSubscriptionHandle> TopicSubscriptions { get; }
    /// <summary>Gets queue declarations keyed by entity name.</summary>
    protected NamedEntityCollection<QueueEntity, QueueHandle> Queues { get; }

    /// <summary>Adds or reuses a topic declaration.</summary>
    /// <param name="createTopicOptions">The Azure topic declaration options.</param>
    /// <returns>A handle to the topology topic.</returns>
    public TopicHandle CreateTopic(CreateTopicOptions createTopicOptions)
    {
        var exchange = new TopicEntity(GetNextId(), createTopicOptions);

        return Topics.GetOrAdd(exchange);
    }

    /// <summary>Adds or reuses a consumer subscription on a topic.</summary>
    /// <param name="topic">The source topic handle.</param>
    /// <param name="createSubscriptionOptions">The Azure subscription declaration options.</param>
    /// <param name="rule">The optional initial subscription rule.</param>
    /// <param name="filter">The optional broker rule filter.</param>
    /// <returns>A handle to the topology subscription.</returns>
    public SubscriptionHandle CreateSubscription(TopicHandle topic, CreateSubscriptionOptions createSubscriptionOptions, CreateRuleOptions? rule,
        RuleFilter? filter)
    {
        var topicEntity = Topics.Get(topic);

        var subscriptionEntity = new SubscriptionEntity(GetNextId(), topicEntity, createSubscriptionOptions, rule, filter);

        return Subscriptions.GetOrAdd(subscriptionEntity);
    }

    /// <summary>Adds or reuses a queue declaration.</summary>
    /// <param name="createQueueOptions">The Azure queue declaration options.</param>
    /// <returns>A handle to the topology queue.</returns>
    public QueueHandle CreateQueue(CreateQueueOptions createQueueOptions)
    {
        var queue = new QueueEntity(GetNextId(), createQueueOptions);

        return Queues.GetOrAdd(queue);
    }

    /// <summary>Creates queue subscription.</summary>
    /// <param name="exchange">The source topic handle.</param>
    /// <param name="queue">The forwarding destination queue.</param>
    /// <param name="createSubscriptionOptions">The Azure forwarding subscription declaration options.</param>
    /// <param name="rule">The optional initial subscription rule.</param>
    /// <param name="filter">The optional broker rule filter.</param>
    /// <returns>A handle to the topic-to-queue relationship.</returns>
    public QueueSubscriptionHandle CreateQueueSubscription(TopicHandle exchange, QueueHandle queue, CreateSubscriptionOptions createSubscriptionOptions,
        CreateRuleOptions? rule, RuleFilter? filter)
    {
        var topicEntity = Topics.Get(exchange);

        var queueEntity = Queues.Get(queue);

        if (topicEntity.CreateTopicOptions.EnablePartitioning)
            queueEntity.CreateQueueOptions.EnablePartitioning = true;

        var binding = new QueueSubscriptionEntity(GetNextId(), GetNextId(), topicEntity, queueEntity, createSubscriptionOptions, rule, filter);

        return QueueSubscriptions.GetOrAdd(binding);
    }

    /// <summary>Creates topic subscription.</summary>
    /// <param name="source">The source topic.</param>
    /// <param name="destination">The forwarding destination topic.</param>
    /// <param name="createSubscriptionOptions">The Azure forwarding subscription declaration options.</param>
    /// <returns>A handle to the topic-to-topic relationship.</returns>
    public TopicSubscriptionHandle CreateTopicSubscription(TopicHandle source, TopicHandle destination, CreateSubscriptionOptions createSubscriptionOptions)
    {
        var sourceEntity = Topics.Get(source);

        var destinationEntity = Topics.Get(destination);

        if (sourceEntity.CreateTopicOptions.EnablePartitioning)
            destinationEntity.CreateTopicOptions.EnablePartitioning = true;

        var subscriptionEntity = new TopicSubscriptionEntity(GetNextId(), GetNextId(), sourceEntity, destinationEntity, createSubscriptionOptions);

        return TopicSubscriptions.GetOrAdd(subscriptionEntity);
    }

    /// <summary>Builds an immutable view over the collected entity relationships.</summary>
    /// <returns>The complete Azure Service Bus broker topology.</returns>
    public BrokerTopology BuildBrokerTopology()
    {
        return new ServiceBusBrokerTopology(Topics, Subscriptions, Queues, QueueSubscriptions, TopicSubscriptions);
    }

    long GetNextId()
    {
        return Interlocked.Increment(ref _nextId);
    }
}
