using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>
/// Provides a consumer entity implementation.
/// </summary>
public class ConsumerEntity :
    Consumer,
    ConsumerHandle
{
    readonly QueueEntity? _queue;
    readonly TopicEntity _topic;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="id">The id value.</param>
    /// <param name="topic">The topic value.</param>
    /// <param name="queue">The queue value.</param>
    /// <param name="selector">The selector value.</param>
    /// <param name="consumerName">The consumer name value.</param>
    /// <param name="shared">The shared value.</param>
    public ConsumerEntity(long id, TopicEntity topic, QueueEntity? queue, string? selector, string? consumerName, bool shared)
        : this(id, topic, queue, selector)
    {
        ConsumerName = consumerName;
        IsShared = shared;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="id">The id value.</param>
    /// <param name="topic">The topic value.</param>
    /// <param name="queue">The queue value.</param>
    /// <param name="selector">The selector value.</param>
    public ConsumerEntity(long id, TopicEntity topic, QueueEntity? queue, string? selector)
    {
        Id = id;
        Selector = selector;
        _topic = topic;
        _queue = queue;
    }

    /// <summary>
    /// Gets the name comparer value.
    /// </summary>
    public static IEqualityComparer<ConsumerEntity> NameComparer { get; } = new NameEqualityComparer();
    /// <summary>
    /// Gets the entity comparer value.
    /// </summary>
    public static IEqualityComparer<ConsumerEntity> EntityComparer { get; } = new ConsumerEntityEqualityComparer();

    /// <summary>
    /// Gets the source value.
    /// </summary>
    public Topic Source => _topic.Topic;
    /// <summary>
    /// Gets the destination value.
    /// </summary>
    public Queue? Destination => _queue?.Queue;
    /// <summary>
    /// Gets the selector value.
    /// </summary>
    public string? Selector { get; }
    /// <summary>
    /// Gets the consumer name value.
    /// </summary>
    public string? ConsumerName { get; }
    /// <summary>
    /// Gets the is shared value.
    /// </summary>
    public bool IsShared { get; }

    /// <summary>
    /// Gets the id value.
    /// </summary>
    public long Id { get; }
    /// <summary>
    /// Gets the consumer value.
    /// </summary>
    public Consumer Consumer => this;

    /// <summary>
    /// Returns the string representation of this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
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
