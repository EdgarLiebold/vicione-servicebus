using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.RabbitMq.Topology;

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
    /// <param name="exclusive">The exclusive value.</param>
    /// <param name="arguments">The arguments value.</param>
    public QueueEntity(long id, string name, bool durable, bool autoDelete, bool exclusive, IDictionary<string, object?> arguments)
    {
        Id = id;
        QueueName = name;
        Durable = durable;
        AutoDelete = autoDelete;
        Exclusive = exclusive;
        QueueArguments = arguments ?? new Dictionary<string, object?>();
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
    /// Gets the queue name value.
    /// </summary>
    public string QueueName { get; }
    /// <summary>
    /// Gets the durable value.
    /// </summary>
    public bool Durable { get; }
    /// <summary>
    /// Gets the auto delete value.
    /// </summary>
    public bool AutoDelete { get; }
    /// <summary>
    /// Gets the exclusive value.
    /// </summary>
    public bool Exclusive { get; }
    /// <summary>
    /// Gets the queue arguments value.
    /// </summary>
    public IDictionary<string, object?> QueueArguments { get; }
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
