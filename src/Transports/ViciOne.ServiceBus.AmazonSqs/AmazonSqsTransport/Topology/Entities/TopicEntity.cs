using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>
/// Provides a topic entity implementation.
/// </summary>
public class TopicEntity :
    Topic,
    TopicHandle
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="id">The id value.</param>
    /// <param name="name">The name value.</param>
    /// <param name="durable">The durable value.</param>
    /// <param name="autoDelete">The auto delete value.</param>
    /// <param name="topicAttributes">The topic attributes value.</param>
    /// <param name="topicSubscriptionAttributes">The topic subscription attributes value.</param>
    /// <param name="topicTags">The topic tags value.</param>
    public TopicEntity(long id, string name, bool durable, bool autoDelete, IDictionary<string, object>? topicAttributes = null,
        IDictionary<string, object>? topicSubscriptionAttributes = null, IDictionary<string, string>? topicTags = null)
    {
        Id = id;
        EntityName = name;
        Durable = durable;
        AutoDelete = autoDelete;
        TopicAttributes = topicAttributes ?? new Dictionary<string, object>();
        TopicSubscriptionAttributes = topicSubscriptionAttributes ?? new Dictionary<string, object>();
        TopicTags = topicTags ?? new Dictionary<string, string>();

        EnsureRawDeliveryIsSet();
    }

    /// <summary>
    /// Gets the name comparer value.
    /// </summary>
    public static IEqualityComparer<TopicEntity> NameComparer { get; } = new NameEqualityComparer();

    /// <summary>
    /// Gets the entity comparer value.
    /// </summary>
    public static IEqualityComparer<TopicEntity> EntityComparer { get; } = new TopicEntityEqualityComparer();

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
    /// Gets the topic attributes value.
    /// </summary>
    public IDictionary<string, object> TopicAttributes { get; }
    /// <summary>
    /// Gets the topic subscription attributes value.
    /// </summary>
    public IDictionary<string, object> TopicSubscriptionAttributes { get; }
    /// <summary>
    /// Gets the topic tags value.
    /// </summary>
    public IDictionary<string, string> TopicTags { get; }
    /// <summary>
    /// Gets the id value.
    /// </summary>
    public long Id { get; }
    /// <summary>
    /// Gets the topic value.
    /// </summary>
    public Topic Topic => this;

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
                TopicTags.Any() ? $"tags: {string.Join(";", TopicTags.Select(a => $"{a.Key}={a.Value}"))}" : "",
                TopicAttributes.Any() ? $"attributes: {string.Join(";", TopicAttributes.Select(a => $"{a.Key}={a.Value}"))}" : "",
                TopicSubscriptionAttributes.Any()
                    ? $"subscription-attributes: {string.Join(";", TopicSubscriptionAttributes.Select(a => $"{a.Key}={a.Value}"))}"
                    : ""
            }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    void EnsureRawDeliveryIsSet()
    {
        TopicSubscriptionAttributes["RawMessageDelivery"] = "true";
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
