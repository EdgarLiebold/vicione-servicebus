using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>Represents the queue subscription topology entity.</summary>
public class QueueSubscriptionEntity :
    TopicToQueueSubscription,
    QueueSubscriptionHandle
{
    readonly QueueEntity _queue;
    readonly TopicEntity _topic;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="id">The id.</param>
    /// <param name="topic">The topic.</param>
    /// <param name="queue">The queue.</param>
    /// <param name="subscriptionType">The runtime subscription type used by the operation.</param>
    /// <param name="routingKey">The routing key.</param>
    public QueueSubscriptionEntity(long id, TopicEntity topic, QueueEntity queue, SqlSubscriptionType subscriptionType, string? routingKey)
    {
        Id = id;
        SubscriptionType = subscriptionType;
        RoutingKey = routingKey;
        _topic = topic;
        _queue = queue;
    }

    /// <summary>Gets the entity comparer.</summary>
    public static IEqualityComparer<QueueSubscriptionEntity> EntityComparer { get; } = new QueueSubscriptionEntityEqualityComparer();

    /// <summary>Gets the id.</summary>
    public long Id { get; }
    /// <summary>Gets the subscription.</summary>
    public TopicToQueueSubscription Subscription => this;
    /// <summary>Gets the subscription type.</summary>
    public SqlSubscriptionType SubscriptionType { get; }

    /// <summary>Gets the source.</summary>
    public Topic Source => _topic.Topic;
    /// <summary>Gets the destination.</summary>
    public Queue Destination => _queue.Queue;
    /// <summary>Gets the routing key.</summary>
    public string? RoutingKey { get; }

    /// <summary>Returns the string representation of this instance.</summary>
    /// <returns>The converted string.</returns>
    public override string ToString()
    {
        return string.Join(", ",
            new[]
            {
                $"source: {Source.TopicName}",
                $"destination: {Destination.QueueName}",
                $"type: {SubscriptionType}",
                string.IsNullOrWhiteSpace(RoutingKey) ? "" : $"routing-key: {RoutingKey}",
            }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }


    sealed class QueueSubscriptionEntityEqualityComparer : IEqualityComparer<QueueSubscriptionEntity>
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
            return QueueEntity.QueueComparer.Equals(x._queue, y._queue)
                && TopicEntity.EntityComparer.Equals(x._topic, y._topic)
                && x.SubscriptionType == y.SubscriptionType
                && x.RoutingKey == y.RoutingKey;
        }

        public int GetHashCode(QueueSubscriptionEntity obj)
        {
            unchecked
            {
                var hashCode = QueueEntity.QueueComparer.GetHashCode(obj._queue);
                hashCode = (hashCode * 397) ^ TopicEntity.EntityComparer.GetHashCode(obj._topic);
                hashCode = (hashCode * 397) ^ (int)obj.SubscriptionType;
                hashCode = (hashCode * 397) ^ (obj.RoutingKey != null ? obj.RoutingKey.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
