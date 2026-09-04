using System.Collections.Generic;
using System.Linq;

#nullable enable
namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>
/// Provides a topic subscription entity implementation.
/// </summary>
public class TopicSubscriptionEntity :
    TopicToTopicSubscription,
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
    /// <param name="subscriptionType">The subscription type value.</param>
    /// <param name="routingKey">The routing key value.</param>
    public TopicSubscriptionEntity(long id, TopicEntity source, TopicEntity destination, SqlSubscriptionType subscriptionType, string? routingKey)
    {
        Id = id;
        SubscriptionType = subscriptionType;
        RoutingKey = routingKey;
        _source = source;
        _destination = destination;
    }

    /// <summary>
    /// Gets the entity comparer value.
    /// </summary>
    public static IEqualityComparer<TopicSubscriptionEntity> EntityComparer { get; } = new TopicSubscriptionEntityEqualityComparer();
    /// <summary>
    /// Gets the id value.
    /// </summary>
    public long Id { get; }
    /// <summary>
    /// Gets the subscription value.
    /// </summary>
    public TopicToTopicSubscription Subscription => this;
    /// <summary>
    /// Gets the subscription type value.
    /// </summary>
    public SqlSubscriptionType SubscriptionType { get; }

    /// <summary>
    /// Gets the source value.
    /// </summary>
    public Topic Source => _source.Topic;
    /// <summary>
    /// Gets the destination value.
    /// </summary>
    public Topic Destination => _destination.Topic;
    /// <summary>
    /// Gets the routing key value.
    /// </summary>
    public string? RoutingKey { get; }

    /// <summary>
    /// Returns the string representation of this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override string ToString()
    {
        return string.Join(", ",
            new[]
            {
                $"source: {Source.TopicName}",
                $"destination: {Destination.TopicName}",
                $"type: {SubscriptionType}",
                string.IsNullOrWhiteSpace(RoutingKey) ? "" : $"routing-key: {RoutingKey}"
            }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }


    sealed class TopicSubscriptionEntityEqualityComparer : IEqualityComparer<TopicSubscriptionEntity>
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
            return x._source.Equals(y._source) && x.SubscriptionType == y.SubscriptionType && x.RoutingKey == y.RoutingKey;
        }

        public int GetHashCode(TopicSubscriptionEntity obj)
        {
            unchecked
            {
                var hashCode = obj._source.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)obj.SubscriptionType;
                hashCode = (hashCode * 397) ^ (obj.RoutingKey != null ? obj.RoutingKey.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
