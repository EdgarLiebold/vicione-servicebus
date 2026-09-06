using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>Represents a de-duplicated ActiveMQ consumer binding.</summary>
public class ConsumerEntity :
    Consumer,
    ConsumerHandle
{
    readonly QueueEntity? _queue;
    readonly TopicEntity _topic;

    /// <summary>Creates a consumer binding with optional named-subscription settings.</summary>
    /// <param name="id">The builder-local entity identifier.</param>
    /// <param name="topic">The source topic entity.</param>
    /// <param name="queue">The destination queue, or <see langword="null" /> for direct topic consumption.</param>
    /// <param name="selector">An optional Apache NMS message selector.</param>
    /// <param name="consumerName">An optional native subscription name.</param>
    /// <param name="shared">Whether the named topic subscription is shared.</param>
    public ConsumerEntity(long id, TopicEntity topic, QueueEntity? queue, string? selector, string? consumerName, bool shared)
        : this(id, topic, queue, selector)
    {
        ConsumerName = consumerName;
        IsShared = shared;
    }

    /// <summary>Creates an unnamed, non-shared consumer binding.</summary>
    /// <param name="id">The builder-local entity identifier.</param>
    /// <param name="topic">The source topic entity.</param>
    /// <param name="queue">The destination queue, or <see langword="null" /> for direct topic consumption.</param>
    /// <param name="selector">An optional Apache NMS message selector.</param>
    public ConsumerEntity(long id, TopicEntity topic, QueueEntity? queue, string? selector)
    {
        Id = id;
        Selector = selector;
        _topic = topic;
        _queue = queue;
    }

    /// <summary>Gets the comparer that de-duplicates bindings by consumer destination or topic/subscription name.</summary>
    public static IEqualityComparer<ConsumerEntity> NameComparer { get; } = new NameEqualityComparer();
    /// <summary>Gets the comparer that includes all binding settings.</summary>
    public static IEqualityComparer<ConsumerEntity> EntityComparer { get; } = new ConsumerEntityEqualityComparer();

    /// <summary>Gets the source topic.</summary>
    public Topic Source => _topic.Topic;
    /// <summary>Gets the destination queue, or <see langword="null" /> for direct topic consumption.</summary>
    public Queue? Destination => _queue?.Queue;
    /// <summary>Gets the Apache NMS message selector.</summary>
    public string? Selector { get; }
    /// <summary>Gets the native subscription name.</summary>
    public string? ConsumerName { get; }
    /// <summary>Gets whether the named topic subscription is shared.</summary>
    public bool IsShared { get; }

    /// <summary>Gets the builder-local entity identifier.</summary>
    public long Id { get; }
    /// <summary>Gets this entity as its consumer-binding contract.</summary>
    public Consumer Consumer => this;

    /// <summary>Returns the non-empty source, destination, selector, and consumer-name settings.</summary>
    /// <returns>A comma-separated diagnostic representation.</returns>
    public override string ToString()
    {
        return string.Join(", ",
            new[]
            {
                $"source: {Source.EntityName}",
                $"destination: {Destination?.EntityName}",
                string.IsNullOrWhiteSpace(Selector) ? "" : $"selector: {Selector}",
                string.IsNullOrWhiteSpace(ConsumerName) ? "" : $"consumerName: {ConsumerName}"
            }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }


    sealed class ConsumerEntityEqualityComparer : IEqualityComparer<ConsumerEntity>
    {
        public bool Equals(ConsumerEntity? x, ConsumerEntity? y)
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
                && ((x._queue != null && QueueEntity.QueueComparer.Equals(x._queue, y._queue)) || (x._queue == null && y._queue == null))
                && string.Equals(x.Selector, y.Selector)
                && string.Equals(x.ConsumerName, y.ConsumerName)
                && x.IsShared == y.IsShared;
        }

        public int GetHashCode(ConsumerEntity obj)
        {
            unchecked
            {
                var hashCode = TopicEntity.EntityComparer.GetHashCode(obj._topic);
                if (obj._queue != null)
                    hashCode = (hashCode * 397) ^ QueueEntity.QueueComparer.GetHashCode(obj._queue);
                if (obj.Selector != null)
                    hashCode = (hashCode * 397) ^ obj.Selector.GetHashCode();
                if (obj.ConsumerName != null)
                    hashCode = (hashCode * 397) ^ obj.ConsumerName.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.IsShared.GetHashCode();

                return hashCode;
            }
        }
    }


    sealed class NameEqualityComparer : IEqualityComparer<ConsumerEntity>
    {
        public bool Equals(ConsumerEntity? x, ConsumerEntity? y)
        {
            if (ReferenceEquals(x, y))
                return true;

            if (ReferenceEquals(x, null))
                return false;

            if (ReferenceEquals(y, null))
                return false;

            if (x.GetType() != y.GetType())
                return false;

            return x._queue == null && y._queue == null
                ? string.Equals(x._topic.EntityName, y._topic.EntityName)
                    && string.Equals(x.ConsumerName, y.ConsumerName)
                : string.Equals(x._queue?.EntityName, y._queue?.EntityName);
        }

        public int GetHashCode(ConsumerEntity obj)
        {
            return obj._queue == null
                ? CombineHashCodes(obj._topic.EntityName, obj.ConsumerName)
                : obj._queue.EntityName.GetHashCode();
        }

        static int CombineHashCodes(string topicName, string? consumerName)
        {
            unchecked
            {
                return (topicName.GetHashCode() * 397) ^ (consumerName?.GetHashCode() ?? 0);
            }
        }
    }
}
