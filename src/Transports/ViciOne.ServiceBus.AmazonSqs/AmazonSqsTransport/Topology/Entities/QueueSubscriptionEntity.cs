using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>Represents a deduplicated Amazon SNS-to-SQS subscription topology entity.</summary>
public class QueueSubscriptionEntity :
    QueueSubscription,
    QueueSubscriptionHandle
{
    readonly QueueEntity _queue;
    readonly TopicEntity _topic;

    /// <summary>Initializes a topic-to-queue subscription entity.</summary>
    /// <param name="id">The builder-assigned entity identifier.</param>
    /// <param name="topic">The source topic entity.</param>
    /// <param name="queue">The destination queue entity.</param>
    public QueueSubscriptionEntity(long id, TopicEntity topic, QueueEntity queue)
    {
        Id = id;
        _topic = topic;
        _queue = queue;
    }

    /// <summary>Gets the collection's subscription-name comparer.</summary>
    public static IEqualityComparer<QueueSubscriptionEntity> NameComparer { get; } = new NameEqualityComparer();
    /// <summary>Gets a comparer that identifies subscriptions by source and destination entities.</summary>
    public static IEqualityComparer<QueueSubscriptionEntity> EntityComparer { get; } = new ConsumerEntityEqualityComparer();

    /// <inheritdoc />
    public Topic Source => _topic.Topic;
    /// <inheritdoc />
    public Queue Destination => _queue.Queue;

    /// <summary>Gets the builder-assigned entity identifier.</summary>
    public long Id { get; }
    /// <summary>Gets this entity as a queue subscription declaration.</summary>
    public QueueSubscription QueueSubscription => this;

    /// <summary>Formats source and destination names for diagnostics.</summary>
    /// <returns>The diagnostic subscription description.</returns>
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

            return string.Equals(x._topic.EntityName, y._topic.EntityName, StringComparison.Ordinal)
                && string.Equals(x._queue.EntityName, y._queue.EntityName, StringComparison.Ordinal);
        }

        public int GetHashCode(QueueSubscriptionEntity obj)
        {
            return HashCode.Combine(
                StringComparer.Ordinal.GetHashCode(obj._topic.EntityName),
                StringComparer.Ordinal.GetHashCode(obj._queue.EntityName));
        }
    }
}
