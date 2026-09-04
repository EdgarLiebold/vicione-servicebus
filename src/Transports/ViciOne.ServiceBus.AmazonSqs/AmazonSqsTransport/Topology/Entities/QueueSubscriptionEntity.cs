using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>
/// Provides a queue subscription entity implementation.
/// </summary>
public class QueueSubscriptionEntity :
    QueueSubscription,
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
    public QueueSubscriptionEntity(long id, TopicEntity topic, QueueEntity queue)
    {
        Id = id;
        _topic = topic;
        _queue = queue;
    }

    /// <summary>
    /// Gets the name comparer value.
    /// </summary>
    public static IEqualityComparer<QueueSubscriptionEntity> NameComparer { get; } = new NameEqualityComparer();
    /// <summary>
    /// Gets the entity comparer value.
    /// </summary>
    public static IEqualityComparer<QueueSubscriptionEntity> EntityComparer { get; } = new ConsumerEntityEqualityComparer();

    /// <summary>
    /// Gets the source value.
    /// </summary>
    public Topic Source => _topic.Topic;
    /// <summary>
    /// Gets the destination value.
    /// </summary>
    public Queue Destination => _queue.Queue;

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
            new[] { $"source: {Source.EntityName}", $"destination: {Destination.EntityName}" }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }


    sealed class ConsumerEntityEqualityComparer : IEqualityComparer<QueueSubscriptionEntity>
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

            return x._topic.Equals(y._topic) && x._queue.Equals(y._queue);
        }

        public int GetHashCode(QueueSubscriptionEntity obj)
        {
            unchecked
            {
                var hashCode = obj._topic.GetHashCode();
                hashCode = (hashCode * 397) ^ obj._queue.GetHashCode();

                return hashCode;
            }
        }
    }


    sealed class NameEqualityComparer : IEqualityComparer<QueueSubscriptionEntity>
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

            return string.Equals(x._topic.EntityName, y._topic.EntityName);
        }

        public int GetHashCode(QueueSubscriptionEntity obj)
        {
            return obj._topic.EntityName.GetHashCode();
        }
    }
}
