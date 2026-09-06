using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>Represents a de-duplicated ActiveMQ queue declaration.</summary>
public class QueueEntity :
    Queue,
    QueueHandle
{
    /// <summary>Creates a queue declaration.</summary>
    /// <param name="id">The builder-local entity identifier.</param>
    /// <param name="name">The queue name.</param>
    /// <param name="durable">Whether the queue persists across broker restarts.</param>
    /// <param name="autoDelete">Whether the broker removes the queue when it is no longer used.</param>
    public QueueEntity(long id, string name, bool durable, bool autoDelete)
    {
        Id = id;
        EntityName = name;
        AutoDelete = autoDelete;
        Durable = durable;
    }

    /// <summary>Gets the comparer that uses the queue name only.</summary>
    public static IEqualityComparer<QueueEntity> NameComparer { get; } = new NameEqualityComparer();

    /// <summary>Gets the comparer that includes name and lifecycle settings.</summary>
    public static IEqualityComparer<QueueEntity> QueueComparer { get; } = new QueueEntityEqualityComparer();

    /// <summary>Gets the queue name.</summary>
    public string EntityName { get; }
    /// <summary>Gets whether the broker removes the queue when it is no longer used.</summary>
    public bool AutoDelete { get; }
    /// <summary>Gets whether the queue persists across broker restarts.</summary>
    public bool Durable { get; }
    /// <summary>Gets the builder-local entity identifier.</summary>
    public long Id { get; }
    /// <summary>Gets this declaration as its queue contract.</summary>
    public Queue Queue => this;

    /// <summary>Returns the queue name and enabled auto-delete flag.</summary>
    /// <returns>A comma-separated diagnostic representation.</returns>
    public override string ToString()
    {
        return string.Join(", ",
            new[] { $"name: {EntityName}", AutoDelete ? "auto-delete" : "" }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }


    sealed class QueueEntityEqualityComparer : IEqualityComparer<QueueEntity>
    {
        public bool Equals(QueueEntity? x, QueueEntity? y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (ReferenceEquals(x, null))
                return false;
            if (ReferenceEquals(y, null))
                return false;
            if (x.GetType() != y.GetType())
                return false;
            return string.Equals(x.EntityName, y.EntityName) && x.AutoDelete == y.AutoDelete && x.Durable == y.Durable;
        }

        public int GetHashCode(QueueEntity obj)
        {
            unchecked
            {
                var hashCode = obj.EntityName.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.AutoDelete.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.Durable.GetHashCode();
                return hashCode;
            }
        }
    }


    sealed class NameEqualityComparer : IEqualityComparer<QueueEntity>
    {
        public bool Equals(QueueEntity? x, QueueEntity? y)
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

        public int GetHashCode(QueueEntity obj)
        {
            return obj.EntityName.GetHashCode();
        }
    }
}
