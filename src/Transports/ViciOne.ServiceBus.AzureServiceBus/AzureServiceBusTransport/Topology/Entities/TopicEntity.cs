using System.Collections.Generic;
using System.Linq;
using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

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
    /// <param name="createTopicOptions">The create topic options value.</param>
    public TopicEntity(long id, CreateTopicOptions createTopicOptions)
    {
        Id = id;

        CreateTopicOptions = createTopicOptions;
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
    /// Gets the create topic options value.
    /// </summary>
    public CreateTopicOptions CreateTopicOptions { get; }
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
        return string.Join(", ", new[] { $"path: {CreateTopicOptions.Name}" }.Where(x => !string.IsNullOrWhiteSpace(x)));
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
            return string.Equals(x.CreateTopicOptions.Name, y.CreateTopicOptions.Name)
                && x.CreateTopicOptions.AutoDeleteOnIdle == y.CreateTopicOptions.AutoDeleteOnIdle
                && x.CreateTopicOptions.DefaultMessageTimeToLive == y.CreateTopicOptions.DefaultMessageTimeToLive
                && x.CreateTopicOptions.DuplicateDetectionHistoryTimeWindow == y.CreateTopicOptions.DuplicateDetectionHistoryTimeWindow
                && x.CreateTopicOptions.EnableBatchedOperations == y.CreateTopicOptions.EnableBatchedOperations
                && x.CreateTopicOptions.EnablePartitioning == y.CreateTopicOptions.EnablePartitioning
                && x.CreateTopicOptions.RequiresDuplicateDetection == y.CreateTopicOptions.RequiresDuplicateDetection
                && x.CreateTopicOptions.SupportOrdering == y.CreateTopicOptions.SupportOrdering
                && string.Equals(x.CreateTopicOptions.UserMetadata, y.CreateTopicOptions.UserMetadata);
        }

        public int GetHashCode(TopicEntity obj)
        {
            unchecked
            {
                var hashCode = obj.CreateTopicOptions.Name.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.CreateTopicOptions.AutoDeleteOnIdle.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.CreateTopicOptions.DefaultMessageTimeToLive.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.CreateTopicOptions.DuplicateDetectionHistoryTimeWindow.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.CreateTopicOptions.EnableBatchedOperations.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.CreateTopicOptions.EnablePartitioning.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.CreateTopicOptions.RequiresDuplicateDetection.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.CreateTopicOptions.SupportOrdering.GetHashCode();
                if (!string.IsNullOrWhiteSpace(obj.CreateTopicOptions.UserMetadata))
                    hashCode = (hashCode * 397) ^ obj.CreateTopicOptions.UserMetadata.GetHashCode();

                return hashCode;
            }
        }
    }


    sealed class NameEqualityComparer :
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
            return string.Equals(x.CreateTopicOptions.Name, y.CreateTopicOptions.Name);
        }

        public int GetHashCode(TopicEntity obj)
        {
            return obj.CreateTopicOptions.Name.GetHashCode();
        }
    }
}
