using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>
/// Provides a queue entity implementation.
/// </summary>
public class QueueEntity :
    Queue,
    QueueHandle
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="id">The id value.</param>
    /// <param name="name">The name value.</param>
    /// <param name="durable">The durable value.</param>
    /// <param name="autoDelete">The auto delete value.</param>
    public QueueEntity(long id, string name, bool durable, bool autoDelete)
    {
        Id = id;
        EntityName = name;
        AutoDelete = autoDelete;
        Durable = durable;
    }

    /// <summary>
    /// Gets the name comparer value.
    /// </summary>
    public static IEqualityComparer<QueueEntity> NameComparer { get; } = new NameEqualityComparer();

    /// <summary>
    /// Gets the queue comparer value.
    /// </summary>
    public static IEqualityComparer<QueueEntity> QueueComparer { get; } = new QueueEntityEqualityComparer();

    /// <summary>
    /// Gets the entity name value.
    /// </summary>
    public string EntityName { get; }
    /// <summary>
    /// Gets the auto delete value.
    /// </summary>
    public bool AutoDelete { get; }
    /// <summary>
    /// Gets the durable value.
    /// </summary>
    public bool Durable { get; }
    /// <summary>
    /// Gets the id value.
    /// </summary>
    public long Id { get; }
    /// <summary>
    /// Gets the queue value.
    /// </summary>
    public Queue Queue => this;

    /// <summary>
    /// Returns the string representation of this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
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
                hashCode = (hashCode * 397) ^ obj.Durable.GetHashCode(); //TODO
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
