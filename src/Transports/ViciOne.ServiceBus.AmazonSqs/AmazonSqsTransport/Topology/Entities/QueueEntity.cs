using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>Represents a deduplicated Amazon SQS queue topology entity.</summary>
public class QueueEntity :
    Queue,
    QueueHandle
{
    /// <summary>Initializes an Amazon SQS queue topology entity.</summary>
    /// <param name="id">The builder-assigned entity identifier.</param>
    /// <param name="name">The queue name.</param>
    /// <param name="durable">Whether the queue is retained when its endpoint stops.</param>
    /// <param name="autoDelete">Whether the queue is deleted when its endpoint stops.</param>
    /// <param name="queueAttributes">Optional Amazon SQS queue attributes.</param>
    /// <param name="queueSubscriptionAttributes">Optional attributes for Amazon SNS subscriptions targeting the queue.</param>
    /// <param name="queueTags">Optional queue tags.</param>
    public QueueEntity(long id, string name, bool durable, bool autoDelete, IDictionary<string, object>? queueAttributes = null,
        IDictionary<string, object>? queueSubscriptionAttributes = null, IDictionary<string, string>? queueTags = null)
    {
        Id = id;
        EntityName = name;
        Durable = durable;
        AutoDelete = autoDelete;
        QueueAttributes = queueAttributes ?? new Dictionary<string, object>();
        QueueSubscriptionAttributes = AmazonSqsAttributeDictionary.CopySubscriptionAttributes(queueSubscriptionAttributes);
        QueueTags = queueTags ?? new Dictionary<string, string>();
    }

    /// <summary>Gets a comparer that identifies queues by name.</summary>
    public static IEqualityComparer<QueueEntity> NameComparer { get; } = new NameEqualityComparer();

    /// <summary>Gets a comparer that identifies queues by name, lifetime, attributes, and tags.</summary>
    public static IEqualityComparer<QueueEntity> QueueComparer { get; } = new QueueEntityEqualityComparer();

    /// <inheritdoc />
    public string EntityName { get; }
    /// <inheritdoc />
    public bool Durable { get; }
    /// <inheritdoc />
    public bool AutoDelete { get; }
    /// <inheritdoc />
    public IDictionary<string, object> QueueAttributes { get; }
    /// <inheritdoc />
    public IDictionary<string, object> QueueSubscriptionAttributes { get; }
    /// <inheritdoc />
    public IDictionary<string, string> QueueTags { get; }
    /// <summary>Gets the builder-assigned entity identifier.</summary>
    public long Id { get; }
    /// <summary>Gets this entity as a queue declaration.</summary>
    public Queue Queue => this;

    /// <summary>Formats the queue name, lifetime, tags, and attributes for diagnostics.</summary>
    /// <returns>The diagnostic queue description.</returns>
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
            return string.Equals(x.EntityName, y.EntityName) && x.Durable == y.Durable && x.AutoDelete == y.AutoDelete
                && AmazonSqsAttributeDictionary.Equivalent(x.QueueAttributes, y.QueueAttributes, StringComparer.Ordinal)
                && AmazonSqsAttributeDictionary.Equivalent(x.QueueSubscriptionAttributes, y.QueueSubscriptionAttributes,
                    StringComparer.OrdinalIgnoreCase)
                && AmazonSqsAttributeDictionary.Equivalent(x.QueueTags, y.QueueTags, StringComparer.Ordinal);
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
            return string.Equals(x.EntityName, y.EntityName);
        }

        public int GetHashCode(QueueEntity obj)
        {
            return obj.EntityName.GetHashCode();
        }
    }
}
