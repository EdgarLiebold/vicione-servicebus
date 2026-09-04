using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.AmazonSqs.Topology;

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
    /// <param name="queueAttributes">The queue attributes value.</param>
    /// <param name="queueSubscriptionAttributes">The queue subscription attributes value.</param>
    /// <param name="queueTags">The queue tags value.</param>
    public QueueEntity(long id, string name, bool durable, bool autoDelete, IDictionary<string, object>? queueAttributes = null,
        IDictionary<string, object>? queueSubscriptionAttributes = null, IDictionary<string, string>? queueTags = null)
    {
        Id = id;
        EntityName = name;
        Durable = durable;
        AutoDelete = autoDelete;
        QueueAttributes = queueAttributes ?? new Dictionary<string, object>();
        QueueSubscriptionAttributes = queueSubscriptionAttributes ?? new Dictionary<string, object>();
        QueueTags = queueTags ?? new Dictionary<string, string>();
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
    /// Gets the durable value.
    /// </summary>
    public bool Durable { get; }
    /// <summary>
    /// Gets the auto delete value.
    /// </summary>
    public bool AutoDelete { get; }
    /// <summary>
    /// Gets the queue attributes value.
    /// </summary>
    public IDictionary<string, object> QueueAttributes { get; }
    /// <summary>
    /// Gets the queue subscription attributes value.
    /// </summary>
    public IDictionary<string, object> QueueSubscriptionAttributes { get; }
    /// <summary>
    /// Gets the queue tags value.
    /// </summary>
    public IDictionary<string, string> QueueTags { get; }
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
                $"name: {EntityName}",
                Durable ? "durable" : "",
                AutoDelete ? "auto-delete" : "",
                QueueTags.Any() ? $"tags: {string.Join(";", QueueTags.Select(a => $"{a.Key}={a.Value}"))}" : "",
                QueueAttributes.Any() ? $"attributes: {string.Join(";", QueueAttributes.Select(a => $"{a.Key}={a.Value}"))}" : "",
                QueueSubscriptionAttributes.Any()
                    ? $"subscription-attributes: {string.Join(";", QueueSubscriptionAttributes.Select(a => $"{a.Key}={a.Value}"))}"
                    : ""
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
            return string.Equals(x.EntityName, y.EntityName) && x.Durable == y.Durable && x.AutoDelete == y.AutoDelete;
        }

        public int GetHashCode(QueueEntity obj)
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
