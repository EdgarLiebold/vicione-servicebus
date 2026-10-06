using System;
using System.Collections.Generic;
using System.Threading;
using Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>Collects deduplicated Azure Service Bus entity declarations and subscriptions.</summary>
public class BrokerTopologyBuilder :
    IBrokerTopologyBuilder
{
    long _nextId;
    readonly Dictionary<(string TopicName, string SubscriptionName), SubscriptionKind> _subscriptionKinds =
        new(new SubscriptionIdentityComparer());

    enum SubscriptionKind
    {
        /// <summary>Identifies a subscription that delivers messages to a consumer endpoint.</summary>
        Consumer,
        /// <summary>Identifies a subscription that forwards messages to a queue.</summary>
        QueueForwarding,
        /// <summary>Identifies a subscription that forwards messages to another topic.</summary>
        TopicForwarding,
    }

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
        ValidateTopicName(topicEntity, createSubscriptionOptions);
        ValidateSessionForwarding(createSubscriptionOptions.RequiresSession, createSubscriptionOptions.ForwardTo,
            nameof(createSubscriptionOptions));
        EnsureSubscriptionKind(createSubscriptionOptions, SubscriptionKind.Consumer);

        var subscriptionEntity = new SubscriptionEntity(GetNextId(), topicEntity, createSubscriptionOptions, rule, filter);

        SubscriptionHandle handle = Subscriptions.GetOrAdd(subscriptionEntity);
        RegisterSubscriptionKind(createSubscriptionOptions, SubscriptionKind.Consumer);
        return handle;
    }

    /// <summary>Adds or reuses a queue declaration.</summary>
    /// <param name="createQueueOptions">The Azure queue declaration options.</param>
    /// <returns>A handle to the topology queue.</returns>
    public QueueHandle CreateQueue(CreateQueueOptions createQueueOptions)
    {
        ArgumentNullException.ThrowIfNull(createQueueOptions);
        ValidateSessionForwarding(createQueueOptions.RequiresSession, createQueueOptions.ForwardTo, nameof(createQueueOptions));
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
        ValidateTopicName(topicEntity, createSubscriptionOptions);
        EnsureSubscriptionKind(createSubscriptionOptions, SubscriptionKind.QueueForwarding);

        var queueEntity = Queues.Get(queue);
        CreateSubscriptionOptions forwardingOptions = ForwardingOptions(createSubscriptionOptions, queueEntity.CreateQueueOptions.Name);

        var binding = new QueueSubscriptionEntity(GetNextId(), GetNextId(), topicEntity, queueEntity, forwardingOptions, rule, filter);

        QueueSubscriptionHandle handle = QueueSubscriptions.GetOrAdd(binding);
        PromoteForwardingDestinations(topicEntity);
        RegisterSubscriptionKind(createSubscriptionOptions, SubscriptionKind.QueueForwarding);
        return handle;
    }

    /// <summary>Creates topic subscription.</summary>
    /// <param name="source">The source topic.</param>
    /// <param name="destination">The forwarding destination topic.</param>
    /// <param name="createSubscriptionOptions">The Azure forwarding subscription declaration options.</param>
    /// <returns>A handle to the topic-to-topic relationship.</returns>
    public TopicSubscriptionHandle CreateTopicSubscription(TopicHandle source, TopicHandle destination, CreateSubscriptionOptions createSubscriptionOptions)
    {
        var sourceEntity = Topics.Get(source);
        ValidateTopicName(sourceEntity, createSubscriptionOptions);
        EnsureSubscriptionKind(createSubscriptionOptions, SubscriptionKind.TopicForwarding);

        var destinationEntity = Topics.Get(destination);
        CreateSubscriptionOptions forwardingOptions = ForwardingOptions(createSubscriptionOptions, destinationEntity.CreateTopicOptions.Name);

        var subscriptionEntity = new TopicSubscriptionEntity(GetNextId(), GetNextId(), sourceEntity, destinationEntity, forwardingOptions);

        TopicSubscriptionHandle handle = TopicSubscriptions.GetOrAdd(subscriptionEntity);
        PromoteForwardingDestinations(sourceEntity);
        RegisterSubscriptionKind(createSubscriptionOptions, SubscriptionKind.TopicForwarding);
        return handle;
    }

    /// <summary>Materializes the collected entity declarations and relationships into a topology with mutable arrays.</summary>
    /// <returns>The complete Azure Service Bus broker topology.</returns>
    public BrokerTopology BuildBrokerTopology()
    {
        return new ServiceBusBrokerTopology(Topics, Subscriptions, Queues, QueueSubscriptions, TopicSubscriptions);
    }

    long GetNextId()
    {
        return Interlocked.Increment(ref _nextId);
    }

    static void ValidateTopicName(TopicEntity topic, CreateSubscriptionOptions createSubscriptionOptions)
    {
        ArgumentNullException.ThrowIfNull(createSubscriptionOptions);

        if (!BrokerName.Equals(topic.CreateTopicOptions.Name, createSubscriptionOptions.TopicName))
            throw new ArgumentException("The subscription topic name does not match its topic handle.", nameof(createSubscriptionOptions));
    }

    static CreateSubscriptionOptions ForwardingOptions(CreateSubscriptionOptions createSubscriptionOptions, string destinationName)
    {
        ValidateSessionForwarding(createSubscriptionOptions.RequiresSession, destinationName, nameof(createSubscriptionOptions));

        if (!string.IsNullOrWhiteSpace(createSubscriptionOptions.ForwardTo)
            && !BrokerName.Equals(createSubscriptionOptions.ForwardTo, destinationName))
            throw new ArgumentException("The subscription forwarding target does not match its destination handle.",
                nameof(createSubscriptionOptions));

        CreateSubscriptionOptions copy = SubscriptionEntity.Snapshot(createSubscriptionOptions);
        copy.ForwardTo = destinationName;
        return copy;
    }

    static void ValidateSessionForwarding(bool requiresSession, string? forwardTo, string parameterName)
    {
        if (requiresSession && !string.IsNullOrWhiteSpace(forwardTo))
            throw new ArgumentException("A session-enabled entity cannot forward messages.", parameterName);
    }

    void PromoteForwardingDestinations(TopicEntity source)
    {
        if (!source.IsPartitioned)
            return;

        var pending = new Queue<TopicEntity>();
        var visited = new HashSet<TopicEntity>(ReferenceEqualityComparer.Instance) { source };
        pending.Enqueue(source);

        while (pending.Count > 0)
        {
            TopicEntity current = pending.Dequeue();
            foreach (QueueSubscriptionEntity relationship in QueueSubscriptions)
                if (ReferenceEquals(relationship.Source, current))
                    ((QueueEntity)relationship.Destination).PromotePartitioning();

            foreach (TopicSubscriptionEntity relationship in TopicSubscriptions)
            {
                if (!ReferenceEquals(relationship.Source, current))
                    continue;

                var destination = (TopicEntity)relationship.Destination;
                destination.PromotePartitioning();
                if (visited.Add(destination))
                    pending.Enqueue(destination);
            }
        }
    }

    void EnsureSubscriptionKind(CreateSubscriptionOptions createSubscriptionOptions, SubscriptionKind kind)
    {
        if (_subscriptionKinds.TryGetValue((createSubscriptionOptions.TopicName, createSubscriptionOptions.SubscriptionName),
            out SubscriptionKind existing) && existing != kind)
            throw new ArgumentException("The subscription broker name is already declared for another relationship kind.",
                nameof(createSubscriptionOptions));
    }

    void RegisterSubscriptionKind(CreateSubscriptionOptions options, SubscriptionKind kind) =>
        _subscriptionKinds.TryAdd((options.TopicName, options.SubscriptionName), kind);

    sealed class SubscriptionIdentityComparer : IEqualityComparer<(string TopicName, string SubscriptionName)>
    {
        public bool Equals((string TopicName, string SubscriptionName) x, (string TopicName, string SubscriptionName) y) =>
            BrokerName.Equals(x.TopicName, y.TopicName)
            && BrokerName.Equals(x.SubscriptionName, y.SubscriptionName);

        public int GetHashCode((string TopicName, string SubscriptionName) identity)
        {
            unchecked
            {
                return (BrokerName.GetHashCode(identity.TopicName) * 397)
                    ^ BrokerName.GetHashCode(identity.SubscriptionName);
            }
        }
    }
}
