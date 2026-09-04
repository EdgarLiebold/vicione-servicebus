using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.ActiveMqTransport.Topology;

public class ConsumerEntity :
    Consumer,
    ConsumerHandle
{
    readonly QueueEntity? _queue;
    readonly TopicEntity _topic;

    public ConsumerEntity(long id, TopicEntity topic, QueueEntity? queue, string? selector, string? consumerName, bool shared)
        : this(id, topic, queue, selector)
    {
        ConsumerName = consumerName;
        IsShared = shared;
    }

    public ConsumerEntity(long id, TopicEntity topic, QueueEntity? queue, string? selector)
    {
        Id = id;
        Selector = selector;
        _topic = topic;
        _queue = queue;
    }

    public static IEqualityComparer<ConsumerEntity> NameComparer { get; } = new NameEqualityComparer();
    public static IEqualityComparer<ConsumerEntity> EntityComparer { get; } = new ConsumerEntityEqualityComparer();

    public Topic Source => _topic.Topic;
    public Queue? Destination => _queue?.Queue;
    public string? Selector { get; }
    public string? ConsumerName { get; }
    public bool IsShared { get; }

    public long Id { get; }
    public Consumer Consumer => this;

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
