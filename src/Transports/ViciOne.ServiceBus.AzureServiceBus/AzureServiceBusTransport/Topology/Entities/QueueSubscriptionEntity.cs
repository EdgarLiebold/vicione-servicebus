using System.Collections.Generic;
using System.Linq;
using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>
/// Provides a queue subscription entity implementation.
/// </summary>
public class QueueSubscriptionEntity :
    QueueSubscription,
    QueueSubscriptionHandle
{
    readonly QueueEntity _queue;
    readonly SubscriptionEntity _subscription;
    readonly TopicEntity _topic;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="id">The id value.</param>
    /// <param name="subscriptionId">The subscription id value.</param>
    /// <param name="topic">The topic value.</param>
    /// <param name="queue">The queue value.</param>
    /// <param name="createSubscriptionOptions">The create subscription options value.</param>
    /// <param name="rule">The rule value.</param>
    /// <param name="filter">The filter value.</param>
    public QueueSubscriptionEntity(long id, long subscriptionId, TopicEntity topic, QueueEntity queue, CreateSubscriptionOptions createSubscriptionOptions,
        CreateRuleOptions? rule = null, RuleFilter? filter = null)
    {
        Id = id;

        _topic = topic;
        _queue = queue;
        _subscription = new SubscriptionEntity(subscriptionId, topic, createSubscriptionOptions, rule, filter);
    }

    /// <summary>
    /// Gets the entity comparer value.
    /// </summary>
    public static IEqualityComparer<QueueSubscriptionEntity> EntityComparer { get; } = new QueueSubscriptionEntityEqualityComparer();
    /// <summary>
    /// Gets the name comparer value.
    /// </summary>
    public static IEqualityComparer<QueueSubscriptionEntity> NameComparer { get; } = new NameEqualityComparer();

    /// <summary>
    /// Gets the source value.
    /// </summary>
    public Topic Source => _topic.Topic;
    /// <summary>
    /// Gets the destination value.
    /// </summary>
    public Queue Destination => _queue.Queue;
    /// <summary>
    /// Gets the subscription value.
    /// </summary>
    public Subscription Subscription => _subscription;

    /// <summary>
    /// Gets the id value.
    /// </summary>
    public long Id { get; }
    /// <summary>
    /// Gets the queue subscription value.
    /// </summary>
    public QueueSubscription QueueSubscription => this;

    /// <summary>
    /// Returns the string representation of this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
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
