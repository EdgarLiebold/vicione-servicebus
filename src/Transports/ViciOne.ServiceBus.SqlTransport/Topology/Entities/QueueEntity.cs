using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>Represents the queue topology entity.</summary>
public class QueueEntity :
    Queue,
    QueueHandle
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="id">The id.</param>
    /// <param name="name">The name.</param>
    /// <param name="autoDeleteOnIdle">The auto delete on idle.</param>
    /// <param name="maxDeliveryCount">The max delivery count.</param>
    public QueueEntity(long id, string name, TimeSpan? autoDeleteOnIdle, int? maxDeliveryCount)
    {
        Id = id;
        QueueName = name;
        AutoDeleteOnIdle = autoDeleteOnIdle;
        MaxDeliveryCount = maxDeliveryCount;
    }

    /// <summary>Gets the name comparer.</summary>
    public static IEqualityComparer<QueueEntity> NameComparer { get; } = new NameEqualityComparer();

    /// <summary>Gets the queue comparer.</summary>
    public static IEqualityComparer<QueueEntity> QueueComparer { get; } = new QueueEntityEqualityComparer();

    /// <summary>Gets the queue name.</summary>
    public string QueueName { get; }
    /// <summary>Gets the auto delete on idle.</summary>
    public TimeSpan? AutoDeleteOnIdle { get; }
    /// <summary>Gets the max delivery count.</summary>
    public int? MaxDeliveryCount { get; }
    /// <summary>Gets the id.</summary>
    public long Id { get; }
    /// <summary>Gets the queue.</summary>
    public Queue Queue => this;

    /// <summary>Returns the string representation of this instance.</summary>
    /// <returns>The converted string.</returns>
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
            return string.Equals(x.QueueName, y.QueueName, StringComparison.Ordinal)
                && x.AutoDeleteOnIdle == y.AutoDeleteOnIdle
                && x.MaxDeliveryCount == y.MaxDeliveryCount;
        }

        public int GetHashCode(QueueEntity obj)
        {
            unchecked
            {
                var hashCode = obj.QueueName.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.AutoDeleteOnIdle.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.MaxDeliveryCount.GetHashCode();

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
            return string.Equals(x.QueueName, y.QueueName, StringComparison.Ordinal);
        }

        public int GetHashCode(QueueEntity obj)
        {
            return obj.QueueName.GetHashCode();
        }
    }
}
