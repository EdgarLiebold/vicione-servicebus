using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>Represents a de-duplicated queue declaration in a broker topology.</summary>
public class QueueEntity :
    Queue,
    QueueHandle
{
    /// <summary>Creates a queue entity.</summary>
    /// <param name="id">The topology-local entity identifier.</param>
    /// <param name="name">The queue name.</param>
    /// <param name="durable">Whether the queue survives broker restarts.</param>
    /// <param name="autoDelete">Whether RabbitMQ deletes the queue after its last consumer is gone.</param>
    /// <param name="exclusive">Whether the queue belongs exclusively to its declaring connection.</param>
    /// <param name="arguments">The broker-specific declaration arguments.</param>
    public QueueEntity(long id, string name, bool durable, bool autoDelete, bool exclusive, IDictionary<string, object?> arguments)
    {
        Id = id;
        QueueName = name;
        Durable = durable;
        AutoDelete = autoDelete;
        Exclusive = exclusive;
        QueueArguments = new ReadOnlyDictionary<string, object?>(
            new Dictionary<string, object?>(arguments ?? new Dictionary<string, object?>()));
    }

    /// <summary>Gets a comparer that considers only the queue name.</summary>
    public static IEqualityComparer<QueueEntity> NameComparer { get; } = new NameEqualityComparer();

    /// <summary>Gets a comparer that includes all queue declaration properties.</summary>
    public static IEqualityComparer<QueueEntity> QueueComparer { get; } = new QueueEntityEqualityComparer();

    /// <summary>Gets the queue name.</summary>
    public string QueueName { get; }
    /// <summary>Gets whether the queue survives broker restarts.</summary>
    public bool Durable { get; }
    /// <summary>Gets whether RabbitMQ deletes the queue after its last consumer is gone.</summary>
    public bool AutoDelete { get; }
    /// <summary>Gets whether the queue belongs exclusively to its declaring connection.</summary>
    public bool Exclusive { get; }
    /// <summary>Gets the broker-specific declaration arguments.</summary>
    public IDictionary<string, object?> QueueArguments { get; }
    /// <summary>Gets the topology-local entity identifier.</summary>
    public long Id { get; }
    /// <summary>Gets this entity as a queue declaration.</summary>
    public Queue Queue => this;

    /// <summary>Formats the queue properties for diagnostics.</summary>
    /// <returns>A diagnostic description of the queue.</returns>
    public override string ToString()
    {
        return string.Join(", ",
            new[]
            {
                $"name: {QueueName}",
                Durable ? "durable" : "",
                AutoDelete ? "auto-delete" : "",
                Exclusive ? "exclusive" : "",
                string.Join(", ", QueueArguments.Select(x => $"{x.Key}: {x.Value}"))
            }.Where(x => !string.IsNullOrWhiteSpace(x)));
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
            return string.Equals(x.QueueName, y.QueueName) && x.Durable == y.Durable && x.AutoDelete == y.AutoDelete && x.Exclusive == y.Exclusive
                && x.QueueArguments.Count == y.QueueArguments.Count
                && x.QueueArguments.All(a => y.QueueArguments.TryGetValue(a.Key, out var value) && Equals(a.Value, value));
        }

        public int GetHashCode(QueueEntity obj)
        {
            unchecked
            {
                var hashCode = obj.QueueName.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.Durable.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.AutoDelete.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.Exclusive.GetHashCode();
                foreach (KeyValuePair<string, object?> keyValuePair in obj.QueueArguments.OrderBy(x => x.Key, System.StringComparer.Ordinal))
                {
                    hashCode = (hashCode * 397) ^ keyValuePair.Key.GetHashCode();
                    hashCode = (hashCode * 397) ^ (keyValuePair.Value?.GetHashCode() ?? 0);
                }

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
            return string.Equals(x.QueueName, y.QueueName);
        }

        public int GetHashCode(QueueEntity obj)
        {
            return obj.QueueName.GetHashCode();
        }
    }
}
