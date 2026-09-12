using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>Represents a deduplicated Amazon SNS topic topology entity.</summary>
public class TopicEntity :
    Topic,
    TopicHandle
{
    /// <summary>Initializes an Amazon SNS topic topology entity and supplies raw delivery as the subscription default.</summary>
    /// <param name="id">The builder-assigned entity identifier.</param>
    /// <param name="name">The topic name.</param>
    /// <param name="durable">Whether the topic is retained when its endpoint stops.</param>
    /// <param name="autoDelete">Whether the topic is deleted when its endpoint stops.</param>
    /// <param name="topicAttributes">Optional Amazon SNS topic attributes.</param>
    /// <param name="topicSubscriptionAttributes">Optional default Amazon SNS subscription attributes.</param>
    /// <param name="topicTags">Optional topic tags.</param>
    public TopicEntity(long id, string name, bool durable, bool autoDelete, IDictionary<string, object>? topicAttributes = null,
        IDictionary<string, object>? topicSubscriptionAttributes = null, IDictionary<string, string>? topicTags = null)
    {
        Id = id;
        EntityName = name;
        Durable = durable;
        AutoDelete = autoDelete;
        TopicAttributes = topicAttributes ?? new Dictionary<string, object>();
        TopicSubscriptionAttributes = AmazonSqsAttributeDictionary.CopySubscriptionAttributes(topicSubscriptionAttributes);
        TopicTags = topicTags ?? new Dictionary<string, string>();

        SetRawDeliveryDefault();
    }

    /// <summary>Gets a comparer that identifies topics by name.</summary>
    public static IEqualityComparer<TopicEntity> NameComparer { get; } = new NameEqualityComparer();

    /// <summary>Gets a comparer that identifies topics by name and lifetime.</summary>
    public static IEqualityComparer<TopicEntity> EntityComparer { get; } = new TopicEntityEqualityComparer();

    /// <inheritdoc />
    public string EntityName { get; }
    /// <inheritdoc />
    public bool Durable { get; }
    /// <inheritdoc />
    public bool AutoDelete { get; }
    /// <inheritdoc />
    public IDictionary<string, object> TopicAttributes { get; }
    /// <inheritdoc />
    public IDictionary<string, object> TopicSubscriptionAttributes { get; }
    /// <inheritdoc />
    public IDictionary<string, string> TopicTags { get; }
    /// <summary>Gets the builder-assigned entity identifier.</summary>
    public long Id { get; }
    /// <summary>Gets this entity as a topic declaration.</summary>
    public Topic Topic => this;

    /// <summary>Formats the topic name, lifetime, tags, and attributes for diagnostics.</summary>
    /// <returns>The diagnostic topic description.</returns>
    public override string ToString()
    {
        return string.Join(", ",
            new[]
            {
                $"name: {EntityName}",
                Durable ? "durable" : "",
                AutoDelete ? "auto-delete" : "",
                TopicTags.Any() ? $"tags: {string.Join(";", TopicTags.Select(a => $"{a.Key}={a.Value}"))}" : "",
                TopicAttributes.Any() ? $"attributes: {string.Join(";", TopicAttributes.Select(a => $"{a.Key}={a.Value}"))}" : "",
                TopicSubscriptionAttributes.Any()
                    ? $"subscription-attributes: {string.Join(";", TopicSubscriptionAttributes.Select(a => $"{a.Key}={a.Value}"))}"
                    : ""
            }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    void SetRawDeliveryDefault()
    {
        TopicSubscriptionAttributes.TryAdd("RawMessageDelivery", "true");
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


    sealed class TopicEntityEqualityComparer :
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
