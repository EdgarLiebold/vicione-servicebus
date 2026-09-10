using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>Represents the topic subscription topology entity.</summary>
public class TopicSubscriptionEntity :
    TopicToTopicSubscription,
    TopicSubscriptionHandle
{
    readonly TopicEntity _destination;
    readonly TopicEntity _source;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="id">The id.</param>
    /// <param name="source">The source value.</param>
    /// <param name="destination">The destination.</param>
    /// <param name="subscriptionType">The runtime subscription type used by the operation.</param>
    /// <param name="routingKey">The routing key.</param>
    public TopicSubscriptionEntity(long id, TopicEntity source, TopicEntity destination, SqlSubscriptionType subscriptionType, string? routingKey)
    {
        Id = id;
        SubscriptionType = subscriptionType;
        RoutingKey = routingKey;
        _source = source;
        _destination = destination;
    }

    /// <summary>Gets the entity comparer.</summary>
    public static IEqualityComparer<TopicSubscriptionEntity> EntityComparer { get; } = new TopicSubscriptionEntityEqualityComparer();
    /// <summary>Gets the id.</summary>
    public long Id { get; }
    /// <summary>Gets the subscription.</summary>
    public TopicToTopicSubscription Subscription => this;
    /// <summary>Gets the subscription type.</summary>
    public SqlSubscriptionType SubscriptionType { get; }

    /// <summary>Gets the source.</summary>
    public Topic Source => _source.Topic;
    /// <summary>Gets the destination.</summary>
    public Topic Destination => _destination.Topic;
    /// <summary>Gets the routing key.</summary>
    public string? RoutingKey { get; }

    /// <summary>Returns the string representation of this instance.</summary>
    /// <returns>The converted string.</returns>
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
            return x._source.Equals(y._source)
                && x._destination.Equals(y._destination)
                && x.SubscriptionType == y.SubscriptionType
                && string.Equals(x.RoutingKey, y.RoutingKey, StringComparison.Ordinal);
        }

        public int GetHashCode(TopicSubscriptionEntity obj)
        {
            unchecked
            {
                var hashCode = obj._source.GetHashCode();
                hashCode = (hashCode * 397) ^ obj._destination.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)obj.SubscriptionType;
                hashCode = (hashCode * 397) ^ (obj.RoutingKey != null ? obj.RoutingKey.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
