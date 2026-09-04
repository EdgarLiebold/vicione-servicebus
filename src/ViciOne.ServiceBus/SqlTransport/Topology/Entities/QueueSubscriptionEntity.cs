using System.Collections.Generic;
using System.Linq;

#nullable enable
namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>
/// Provides a queue subscription entity implementation.
/// </summary>
public class QueueSubscriptionEntity :
    TopicToQueueSubscription,
    QueueSubscriptionHandle
{
    readonly QueueEntity _queue;
    readonly TopicEntity _topic;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="id">The id value.</param>
    /// <param name="topic">The topic value.</param>
    /// <param name="queue">The queue value.</param>
    /// <param name="subscriptionType">The subscription type value.</param>
    /// <param name="routingKey">The routing key value.</param>
    public QueueSubscriptionEntity(long id, TopicEntity topic, QueueEntity queue, SqlSubscriptionType subscriptionType, string? routingKey)
    {
        Id = id;
        SubscriptionType = subscriptionType;
        RoutingKey = routingKey;
        _topic = topic;
        _queue = queue;
    }

    /// <summary>
    /// Gets the entity comparer value.
    /// </summary>
    public static IEqualityComparer<QueueSubscriptionEntity> EntityComparer { get; } = new QueueSubscriptionEntityEqualityComparer();

    /// <summary>
    /// Gets the id value.
    /// </summary>
    public long Id { get; }
    /// <summary>
    /// Gets the subscription value.
    /// </summary>
    public TopicToQueueSubscription Subscription => this;
    /// <summary>
    /// Gets the subscription type value.
    /// </summary>
    public SqlSubscriptionType SubscriptionType { get; }

    /// <summary>
    /// Gets the source value.
    /// </summary>
    public Topic Source => _topic.Topic;
    /// <summary>
    /// Gets the destination value.
    /// </summary>
    public Queue Destination => _queue.Queue;
    /// <summary>
    /// Gets the routing key value.
    /// </summary>
    public string? RoutingKey { get; }

    /// <summary>
    /// Returns the string representation of this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
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
            return x._queue.Equals(y._queue) && x._topic.Equals(y._topic) && x.SubscriptionType == y.SubscriptionType && x.RoutingKey == y.RoutingKey;
        }

        public int GetHashCode(QueueSubscriptionEntity obj)
        {
            unchecked
            {
                var hashCode = obj._queue.GetHashCode();
                hashCode = (hashCode * 397) ^ obj._topic.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)obj.SubscriptionType;
                hashCode = (hashCode * 397) ^ (obj.RoutingKey != null ? obj.RoutingKey.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
