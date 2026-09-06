using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>Represents a de-duplicated ActiveMQ topic declaration.</summary>
public class TopicEntity :
    Topic,
    TopicHandle
{
    /// <summary>Creates a topic declaration.</summary>
    /// <param name="id">The builder-local entity identifier.</param>
    /// <param name="name">The topic name.</param>
    /// <param name="durable">Whether the topic persists across broker restarts.</param>
    /// <param name="autoDelete">Whether the broker removes the topic when it is no longer used.</param>
    public TopicEntity(long id, string name, bool durable, bool autoDelete)
    {
        Id = id;
        EntityName = name;
        Durable = durable;
        AutoDelete = autoDelete;
    }

    /// <summary>Gets the comparer that uses the topic name only.</summary>
    public static IEqualityComparer<TopicEntity> NameComparer { get; } = new NameEqualityComparer();

    /// <summary>Gets the comparer that includes name and lifecycle settings.</summary>
    public static IEqualityComparer<TopicEntity> EntityComparer { get; } = new ExchangeEntityEqualityComparer();

    /// <summary>Gets the topic name.</summary>
    public string EntityName { get; }
    /// <summary>Gets whether the topic persists across broker restarts.</summary>
    public bool Durable { get; }
    /// <summary>Gets whether the broker removes the topic when it is no longer used.</summary>
    public bool AutoDelete { get; }
    /// <summary>Gets the builder-local entity identifier.</summary>
    public long Id { get; }
    /// <summary>Gets this declaration as its topic contract.</summary>
    public Topic Topic => this;

    /// <summary>Returns the topic name and enabled lifecycle flags.</summary>
    /// <returns>A comma-separated diagnostic representation.</returns>
    public override string ToString()
    {
        return string.Join(", ",
            new[] { $"name: {EntityName}", Durable ? "durable" : "", AutoDelete ? "auto-delete" : "" }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }


    sealed class NameEqualityComparer : IEqualityComparer<TopicEntity>
    {
        public bool Equals(TopicEntity? x, TopicEntity? y)
        {
            if (ReferenceEquals(x, y))
                return true;

            if (ReferenceEquals(x, null))
                return false;

            if (ReferenceEquals(y, null))
                return false;

            if (x.GetType() != y.GetType())
                return false;

            return string.Equals(x.EntityName, y.EntityName);
        }

        public int GetHashCode(TopicEntity obj)
        {
            return obj.EntityName.GetHashCode();
        }
    }


    sealed class ExchangeEntityEqualityComparer :
        IEqualityComparer<TopicEntity>
    {
        public bool Equals(TopicEntity? x, TopicEntity? y)
        {
            if (ReferenceEquals(x, y))
                return true;

            if (ReferenceEquals(x, null))
                return false;

            if (ReferenceEquals(y, null))
                return false;

            if (x.GetType() != y.GetType())
                return false;

            return string.Equals(x.EntityName, y.EntityName) && x.Durable == y.Durable && x.AutoDelete == y.AutoDelete;
        }

        public int GetHashCode(TopicEntity obj)
        {
            unchecked
            {
                var hashCode = obj.EntityName.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.Durable.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.AutoDelete.GetHashCode();
                return hashCode;
            }
        }
    }
}
