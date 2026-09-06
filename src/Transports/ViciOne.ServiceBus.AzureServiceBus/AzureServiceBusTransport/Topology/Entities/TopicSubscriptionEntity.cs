using System.Collections.Generic;
using System.Linq;
using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>Represents a topic subscription that forwards matching messages to another topic.</summary>
public class TopicSubscriptionEntity :
    TopicSubscription,
    TopicSubscriptionHandle
{
    readonly TopicEntity _destination;
    readonly TopicEntity _source;
    readonly SubscriptionEntity _subscription;

    /// <summary>Creates a topology relationship from a source topic to a destination topic.</summary>
    /// <param name="id">The relationship's topology-local identifier.</param>
    /// <param name="subscriptionId">The forwarding subscription's topology-local identifier.</param>
    /// <param name="source">The source topic.</param>
    /// <param name="destination">The forwarding destination topic.</param>
    /// <param name="createSubscriptionOptions">The Azure forwarding subscription declaration options.</param>
    /// <param name="rule">The optional initial subscription rule.</param>
    /// <param name="filter">The optional broker rule filter.</param>
    public TopicSubscriptionEntity(long id, long subscriptionId, TopicEntity source, TopicEntity destination,
        CreateSubscriptionOptions createSubscriptionOptions, CreateRuleOptions? rule = null, RuleFilter? filter = null)
    {
        Id = id;
        _source = source;
        _destination = destination;
        _subscription = new SubscriptionEntity(subscriptionId, source, createSubscriptionOptions, rule, filter);
    }

    /// <summary>Gets a comparer that considers complete source, destination, and subscription declarations.</summary>
    public static IEqualityComparer<TopicSubscriptionEntity> EntityComparer { get; } = new TopicSubscriptionEntityEqualityComparer();
    /// <summary>Gets a comparer that considers source, destination, and subscription names.</summary>
    public static IEqualityComparer<TopicSubscriptionEntity> NameComparer { get; } = new NameEqualityComparer();

    /// <summary>Gets the source topic.</summary>
    public Topic Source => _source.Topic;
    /// <summary>Gets the forwarding destination topic.</summary>
    public Topic Destination => _destination.Topic;
    /// <summary>Gets the forwarding subscription.</summary>
    public Subscription Subscription => _subscription.Subscription;

    /// <summary>Gets the relationship's topology-local identifier.</summary>
    public long Id { get; }
    /// <summary>Gets this entity through the read-only topic-subscription contract.</summary>
    public TopicSubscription TopicSubscription => this;

    /// <summary>Formats the source topic, destination topic, and forwarding subscription for diagnostics.</summary>
    /// <returns>A diagnostic string containing source, destination, and subscription names.</returns>
    public override string ToString()
    {
        return string.Join(", ",
            new[]
            {
                $"source: {_source.CreateTopicOptions.Name}",
                $"destination: {_destination.CreateTopicOptions.Name}",
                $"subscription: {_subscription.CreateSubscriptionOptions.SubscriptionName}"
            }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }


    sealed class TopicSubscriptionEntityEqualityComparer :
        IEqualityComparer<TopicSubscriptionEntity>
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

            return TopicEntity.EntityComparer.Equals(x._source, y._source)
                && TopicEntity.EntityComparer.Equals(x._destination, y._destination)
                && SubscriptionEntity.EntityComparer.Equals(x._subscription, y._subscription);
        }

        public int GetHashCode(TopicSubscriptionEntity obj)
        {
            unchecked
            {
                var hashCode = TopicEntity.EntityComparer.GetHashCode(obj._source);
                hashCode = (hashCode * 397) ^ TopicEntity.EntityComparer.GetHashCode(obj._destination);
                hashCode = (hashCode * 397) ^ SubscriptionEntity.EntityComparer.GetHashCode(obj._subscription);

                return hashCode;
            }
        }
    }


    sealed class NameEqualityComparer :
        IEqualityComparer<TopicSubscriptionEntity>
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

            return string.Equals(x.Subscription.CreateSubscriptionOptions.SubscriptionName, y.Subscription.CreateSubscriptionOptions.SubscriptionName)
                && string.Equals(x.Subscription.CreateSubscriptionOptions.TopicName, y.Subscription.CreateSubscriptionOptions.TopicName)
                && string.Equals(x.Destination.CreateTopicOptions.Name, y.Destination.CreateTopicOptions.Name);
        }

        public int GetHashCode(TopicSubscriptionEntity obj)
        {
            var hashCode = obj.Subscription.CreateSubscriptionOptions.SubscriptionName.GetHashCode();
            hashCode = (hashCode * 397) ^ obj.Subscription.CreateSubscriptionOptions.TopicName.GetHashCode();
            hashCode = (hashCode * 397) ^ obj.Destination.CreateTopicOptions.Name.GetHashCode();

            return hashCode;
        }
    }
}
