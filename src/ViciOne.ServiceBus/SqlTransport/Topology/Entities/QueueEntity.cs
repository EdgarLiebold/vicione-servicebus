using System;
using System.Collections.Generic;
using System.Linq;

#nullable enable
namespace ViciOne.ServiceBus.SqlTransport.Topology;

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
    /// <param name="autoDeleteOnIdle">The auto delete on idle value.</param>
    /// <param name="maxDeliveryCount">The max delivery count value.</param>
    public QueueEntity(long id, string name, TimeSpan? autoDeleteOnIdle, int? maxDeliveryCount)
    {
        Id = id;
        QueueName = name;
        AutoDeleteOnIdle = autoDeleteOnIdle;
        MaxDeliveryCount = maxDeliveryCount;
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
    /// Gets the auto delete on idle value.
    /// </summary>
    public TimeSpan? AutoDeleteOnIdle { get; }
    /// <summary>
    /// Gets the max delivery count value.
    /// </summary>
    public int? MaxDeliveryCount { get; }
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
            new[] { $"name: {QueueName}", AutoDeleteOnIdle.HasValue ? $"auto-delete after {AutoDeleteOnIdle}" : "", }
                .Where(x => !string.IsNullOrWhiteSpace(x)));
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
            return string.Equals(x.QueueName, y.QueueName) && x.AutoDeleteOnIdle == y.AutoDeleteOnIdle;
        }

        public int GetHashCode(QueueEntity obj)
        {
            unchecked
            {
                var hashCode = obj.QueueName.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.AutoDeleteOnIdle.GetHashCode();

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
