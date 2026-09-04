using System.Collections.Generic;
using System.Linq;
using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>
/// Provides a topic subscription entity implementation.
/// </summary>
public class TopicSubscriptionEntity :
    TopicSubscription,
    TopicSubscriptionHandle
{
    readonly TopicEntity _destination;
    readonly TopicEntity _source;
    readonly SubscriptionEntity _subscription;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="id">The id value.</param>
    /// <param name="subscriptionId">The subscription id value.</param>
    /// <param name="source">The source value.</param>
    /// <param name="destination">The destination value.</param>
    /// <param name="createSubscriptionOptions">The create subscription options value.</param>
    /// <param name="rule">The rule value.</param>
    /// <param name="filter">The filter value.</param>
    public TopicSubscriptionEntity(long id, long subscriptionId, TopicEntity source, TopicEntity destination,
        CreateSubscriptionOptions createSubscriptionOptions, CreateRuleOptions? rule = null, RuleFilter? filter = null)
    {
        Id = id;
        _source = source;
        _destination = destination;
        _subscription = new SubscriptionEntity(subscriptionId, source, createSubscriptionOptions, rule, filter);
    }

    /// <summary>
    /// Gets the entity comparer value.
    /// </summary>
    public static IEqualityComparer<TopicSubscriptionEntity> EntityComparer { get; } = new TopicSubscriptionEntityEqualityComparer();
    /// <summary>
    /// Gets the name comparer value.
    /// </summary>
    public static IEqualityComparer<TopicSubscriptionEntity> NameComparer { get; } = new NameEqualityComparer();

    /// <summary>
    /// Gets the source value.
    /// </summary>
    public Topic Source => _source.Topic;
    /// <summary>
    /// Gets the destination value.
    /// </summary>
    public Topic Destination => _destination.Topic;
    /// <summary>
    /// Gets the subscription value.
    /// </summary>
    public Subscription Subscription => _subscription.Subscription;

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
