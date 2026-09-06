using System.Collections.Generic;
using System.Linq;
using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>Represents a topic subscription that forwards matching messages to a queue.</summary>
public class QueueSubscriptionEntity :
    QueueSubscription,
    QueueSubscriptionHandle
{
    readonly QueueEntity _queue;
    readonly SubscriptionEntity _subscription;
    readonly TopicEntity _topic;

    /// <summary>Creates a topology relationship from a source topic to a destination queue.</summary>
    /// <param name="id">The relationship's topology-local identifier.</param>
    /// <param name="subscriptionId">The forwarding subscription's topology-local identifier.</param>
    /// <param name="topic">The source topic.</param>
    /// <param name="queue">The forwarding destination queue.</param>
    /// <param name="createSubscriptionOptions">The Azure forwarding subscription declaration options.</param>
    /// <param name="rule">The optional initial subscription rule.</param>
    /// <param name="filter">The optional broker rule filter.</param>
    public QueueSubscriptionEntity(long id, long subscriptionId, TopicEntity topic, QueueEntity queue, CreateSubscriptionOptions createSubscriptionOptions,
        CreateRuleOptions? rule = null, RuleFilter? filter = null)
    {
        Id = id;

        _topic = topic;
        _queue = queue;
        _subscription = new SubscriptionEntity(subscriptionId, topic, createSubscriptionOptions, rule, filter);
    }

    /// <summary>Gets a comparer that considers complete source, destination, and subscription declarations.</summary>
    public static IEqualityComparer<QueueSubscriptionEntity> EntityComparer { get; } = new QueueSubscriptionEntityEqualityComparer();
    /// <summary>Gets a comparer that considers source, destination, and subscription names.</summary>
    public static IEqualityComparer<QueueSubscriptionEntity> NameComparer { get; } = new NameEqualityComparer();

    /// <summary>Gets the source topic.</summary>
    public Topic Source => _topic.Topic;
    /// <summary>Gets the forwarding destination queue.</summary>
    public Queue Destination => _queue.Queue;
    /// <summary>Gets the forwarding subscription.</summary>
    public Subscription Subscription => _subscription;

    /// <summary>Gets the relationship's topology-local identifier.</summary>
    public long Id { get; }
    /// <summary>Gets this entity through the read-only queue-subscription contract.</summary>
    public QueueSubscription QueueSubscription => this;

    /// <summary>Formats the source topic, destination queue, and forwarding subscription for diagnostics.</summary>
    /// <returns>A diagnostic string containing source, destination, and subscription names.</returns>
    public override string ToString()
    {
        return string.Join(", ",
            new[]
            {
                $"topic: {_topic.CreateTopicOptions.Name}",
                $"queue: {_queue.CreateQueueOptions.Name}",
                $"subscription: {_subscription.CreateSubscriptionOptions.SubscriptionName}"
            }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }


    sealed class QueueSubscriptionEntityEqualityComparer :
        IEqualityComparer<QueueSubscriptionEntity>
    {
        public bool Equals(QueueSubscriptionEntity? x, QueueSubscriptionEntity? y)
        {
            if (ReferenceEquals(x, y))
                return true;

            if (ReferenceEquals(x, null))
                return false;

            if (ReferenceEquals(y, null))
                return false;

            if (x.GetType() != y.GetType())
                return false;

            return TopicEntity.EntityComparer.Equals(x._topic, y._topic)
                && QueueEntity.EntityComparer.Equals(x._queue, y._queue)
                && SubscriptionEntity.EntityComparer.Equals(x._subscription, y._subscription);
        }

        public int GetHashCode(QueueSubscriptionEntity obj)
        {
            unchecked
            {
                var hashCode = TopicEntity.EntityComparer.GetHashCode(obj._topic);
                hashCode = (hashCode * 397) ^ QueueEntity.EntityComparer.GetHashCode(obj._queue);
                hashCode = (hashCode * 397) ^ SubscriptionEntity.EntityComparer.GetHashCode(obj._subscription);

                return hashCode;
            }
        }
    }


    sealed class NameEqualityComparer :
        IEqualityComparer<QueueSubscriptionEntity>
    {
        public bool Equals(QueueSubscriptionEntity? x, QueueSubscriptionEntity? y)
        {
            if (ReferenceEquals(x, y))
                return true;

            if (ReferenceEquals(x, null))
                return false;

            if (ReferenceEquals(y, null))
                return false;

            if (x.GetType() != y.GetType())
                return false;

            return string.Equals(x.Subscription.CreateSubscriptionOptions.SubscriptionName, y.Subscription.CreateSubscriptionOptions.SubscriptionName)
                && string.Equals(x.Subscription.CreateSubscriptionOptions.TopicName, y.Subscription.CreateSubscriptionOptions.TopicName)
                && string.Equals(x.Destination.CreateQueueOptions.Name, y.Destination.CreateQueueOptions.Name);
        }

        public int GetHashCode(QueueSubscriptionEntity obj)
        {
            var hashCode = obj.Subscription.CreateSubscriptionOptions.SubscriptionName.GetHashCode();
            hashCode = (hashCode * 397) ^ obj.Subscription.CreateSubscriptionOptions.TopicName.GetHashCode();
            hashCode = (hashCode * 397) ^ obj.Destination.CreateQueueOptions.Name.GetHashCode();

            return hashCode;
        }
    }
}
