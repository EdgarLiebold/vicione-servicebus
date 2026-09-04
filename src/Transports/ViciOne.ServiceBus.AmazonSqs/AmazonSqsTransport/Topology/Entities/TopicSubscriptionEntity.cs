using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>
/// Provides a topic subscription entity implementation.
/// </summary>
public class TopicSubscriptionEntity :
    TopicSubscription,
    TopicSubscriptionHandle
{
    readonly TopicEntity _destination;
    readonly TopicEntity _source;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="id">The id value.</param>
    /// <param name="source">The source value.</param>
    /// <param name="destination">The destination value.</param>
    public TopicSubscriptionEntity(long id, TopicEntity source, TopicEntity destination)
    {
        Id = id;
        _source = source;
        _destination = destination;
    }

    /// <summary>
    /// Gets the name comparer value.
    /// </summary>
    public static IEqualityComparer<TopicSubscriptionEntity> NameComparer { get; } = new NameEqualityComparer();
    /// <summary>
    /// Gets the entity comparer value.
    /// </summary>
    public static IEqualityComparer<TopicSubscriptionEntity> EntityComparer { get; } = new ConsumerEntityEqualityComparer();

    /// <summary>
    /// Gets the source value.
    /// </summary>
    public Topic Source => _source.Topic;
    /// <summary>
    /// Gets the destination value.
    /// </summary>
    public Topic Destination => _destination.Topic;

    /// <summary>
    /// Gets the id value.
    /// </summary>
    public long Id { get; }
    /// <summary>
    /// Gets the topic subscription value.
    /// </summary>
    public TopicSubscription TopicSubscription => this;

    /// <summary>
    /// Returns the string representation of this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override string ToString()
    {
        return string.Join(", ",
            new[] { $"source: {Source.EntityName}", $"destination: {Destination.EntityName}" }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }


    sealed class ConsumerEntityEqualityComparer : IEqualityComparer<TopicSubscriptionEntity>
    {
        public bool Equals(TopicSubscriptionEntity? x, TopicSubscriptionEntity? y)
        {
            if (ReferenceEquals(x, y))
                return true;

            if (ReferenceEquals(x, null))
                return false;

            if (ReferenceEquals(y, null))
                return false;

            if (x.GetType() != y.GetType())
                return false;

            return x._source.Equals(y._source) && x._destination.Equals(y._destination);
        }

        public int GetHashCode(TopicSubscriptionEntity obj)
        {
            unchecked
            {
                var hashCode = obj._source.GetHashCode();
                hashCode = (hashCode * 397) ^ obj._destination.GetHashCode();

                return hashCode;
            }
        }
    }


    sealed class NameEqualityComparer : IEqualityComparer<TopicSubscriptionEntity>
    {
        public bool Equals(TopicSubscriptionEntity? x, TopicSubscriptionEntity? y)
        {
            if (ReferenceEquals(x, y))
                return true;

            if (ReferenceEquals(x, null))
                return false;

            if (ReferenceEquals(y, null))
                return false;

            if (x.GetType() != y.GetType())
                return false;

            return string.Equals(x._source.EntityName, y._source.EntityName);
        }

        public int GetHashCode(TopicSubscriptionEntity obj)
        {
            return obj._source.EntityName.GetHashCode();
        }
    }
}
