using System.Threading;
using Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>
/// Provides a broker topology builder implementation.
/// </summary>
public class BrokerTopologyBuilder :
    IBrokerTopologyBuilder
{
    long _nextId;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
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

    /// <summary>
    /// Gets the subscriptions value.
    /// </summary>
    protected EntityCollection<SubscriptionEntity, SubscriptionHandle> Subscriptions { get; }
    /// <summary>
    /// Gets the topics value.
    /// </summary>
    protected NamedEntityCollection<TopicEntity, TopicHandle> Topics { get; }
    /// <summary>
    /// Gets the queue subscriptions value.
    /// </summary>
    protected EntityCollection<QueueSubscriptionEntity, QueueSubscriptionHandle> QueueSubscriptions { get; }
    /// <summary>
    /// Gets the topic subscriptions value.
    /// </summary>
    protected EntityCollection<TopicSubscriptionEntity, TopicSubscriptionHandle> TopicSubscriptions { get; }
    /// <summary>
    /// Gets the queues value.
    /// </summary>
    protected NamedEntityCollection<QueueEntity, QueueHandle> Queues { get; }

    /// <summary>
    /// Creates topic.
    /// </summary>
    /// <param name="createTopicOptions">The create topic options value.</param>
    /// <returns>The result of the operation.</returns>
    public TopicHandle CreateTopic(CreateTopicOptions createTopicOptions)
    {
        var exchange = new TopicEntity(GetNextId(), createTopicOptions);

        return Topics.GetOrAdd(exchange);
    }

    /// <summary>
    /// Creates subscription.
    /// </summary>
    /// <param name="topic">The topic value.</param>
    /// <param name="createSubscriptionOptions">The create subscription options value.</param>
    /// <param name="rule">The rule value.</param>
    /// <param name="filter">The filter value.</param>
    /// <returns>The result of the operation.</returns>
    public SubscriptionHandle CreateSubscription(TopicHandle topic, CreateSubscriptionOptions createSubscriptionOptions, CreateRuleOptions? rule,
        RuleFilter? filter)
    {
        var topicEntity = Topics.Get(topic);

        var subscriptionEntity = new SubscriptionEntity(GetNextId(), topicEntity, createSubscriptionOptions, rule, filter);

        return Subscriptions.GetOrAdd(subscriptionEntity);
    }

    /// <summary>
    /// Creates queue.
    /// </summary>
    /// <param name="createQueueOptions">The create queue options value.</param>
    /// <returns>The result of the operation.</returns>
    public QueueHandle CreateQueue(CreateQueueOptions createQueueOptions)
    {
        var queue = new QueueEntity(GetNextId(), createQueueOptions);

        return Queues.GetOrAdd(queue);
    }

    /// <summary>
    /// Creates queue subscription.
    /// </summary>
    /// <param name="exchange">The exchange value.</param>
    /// <param name="queue">The queue value.</param>
    /// <param name="createSubscriptionOptions">The create subscription options value.</param>
    /// <param name="rule">The rule value.</param>
    /// <param name="filter">The filter value.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Creates topic subscription.
    /// </summary>
    /// <param name="source">The source value.</param>
    /// <param name="destination">The destination value.</param>
    /// <param name="createSubscriptionOptions">The create subscription options value.</param>
    /// <returns>The result of the operation.</returns>
    public TopicSubscriptionHandle CreateTopicSubscription(TopicHandle source, TopicHandle destination, CreateSubscriptionOptions createSubscriptionOptions)
    {
        var sourceEntity = Topics.Get(source);

        var destinationEntity = Topics.Get(destination);

        if (sourceEntity.CreateTopicOptions.EnablePartitioning)
            destinationEntity.CreateTopicOptions.EnablePartitioning = true;

        var subscriptionEntity = new TopicSubscriptionEntity(GetNextId(), GetNextId(), sourceEntity, destinationEntity, createSubscriptionOptions);

        return TopicSubscriptions.GetOrAdd(subscriptionEntity);
    }

    /// <summary>
    /// Performs the build broker topology operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public BrokerTopology BuildBrokerTopology()
    {
        return new ServiceBusBrokerTopology(Topics, Subscriptions, Queues, QueueSubscriptions, TopicSubscriptions);
    }

    long GetNextId()
    {
        return Interlocked.Increment(ref _nextId);
    }
}
