using System.Collections.Generic;
using System.Linq;
using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>
/// Provides a subscription entity implementation.
/// </summary>
public class SubscriptionEntity :
    Subscription,
    SubscriptionHandle
{
    readonly TopicEntity _topic;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="id">The id value.</param>
    /// <param name="topic">The topic value.</param>
    /// <param name="createSubscriptionOptions">The create subscription options value.</param>
    /// <param name="rule">The rule value.</param>
    /// <param name="filter">The filter value.</param>
    public SubscriptionEntity(long id, TopicEntity topic, CreateSubscriptionOptions createSubscriptionOptions, CreateRuleOptions? rule = null,
        RuleFilter? filter = null)
    {
        Id = id;

        _topic = topic;

        CreateSubscriptionOptions = createSubscriptionOptions;

        Rule = rule;
        Filter = filter;
    }

    /// <summary>
    /// Gets the name comparer value.
    /// </summary>
    public static IEqualityComparer<SubscriptionEntity> NameComparer { get; } = new NameEqualityComparer();
    /// <summary>
    /// Gets the entity comparer value.
    /// </summary>
    public static IEqualityComparer<SubscriptionEntity> EntityComparer { get; } = new SubscriptionEntityEqualityComparer();

    /// <summary>
    /// Gets the create subscription options value.
    /// </summary>
    public CreateSubscriptionOptions CreateSubscriptionOptions { get; }

    /// <summary>
    /// Gets the topic value.
    /// </summary>
    public TopicHandle Topic => _topic;

    /// <summary>
    /// Gets the rule value.
    /// </summary>
    public CreateRuleOptions? Rule { get; }
    /// <summary>
    /// Gets the filter value.
    /// </summary>
    public RuleFilter? Filter { get; }
    /// <summary>
    /// Gets the id value.
    /// </summary>
    public long Id { get; }
    /// <summary>
    /// Gets the subscription value.
    /// </summary>
    public Subscription Subscription => this;

    /// <summary>
    /// Returns the string representation of this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override string ToString()
    {
        return string.Join(", ",
            new[] { $"topic: {_topic.CreateTopicOptions.Name}", $"subscription: {CreateSubscriptionOptions.SubscriptionName}" }.Where(x =>
                !string.IsNullOrWhiteSpace(x)));
    }


    sealed class SubscriptionEntityEqualityComparer :
        IEqualityComparer<SubscriptionEntity>
    {
        public bool Equals(SubscriptionEntity? x, SubscriptionEntity? y)
        {
            if (ReferenceEquals(x, y))
                return true;

            if (ReferenceEquals(x, null))
                return false;

            if (ReferenceEquals(y, null))
                return false;

            if (x.GetType() != y.GetType())
                return false;

            return string.Equals(x.CreateSubscriptionOptions.SubscriptionName, y.CreateSubscriptionOptions.SubscriptionName)
                && string.Equals(x.CreateSubscriptionOptions.TopicName, y.CreateSubscriptionOptions.TopicName)
                && x.CreateSubscriptionOptions.AutoDeleteOnIdle == y.CreateSubscriptionOptions.AutoDeleteOnIdle
                && x.CreateSubscriptionOptions.DefaultMessageTimeToLive == y.CreateSubscriptionOptions.DefaultMessageTimeToLive
                && x.CreateSubscriptionOptions.EnableBatchedOperations == y.CreateSubscriptionOptions.EnableBatchedOperations
                && x.CreateSubscriptionOptions.DeadLetteringOnMessageExpiration == y.CreateSubscriptionOptions.DeadLetteringOnMessageExpiration
                && x.CreateSubscriptionOptions.EnableDeadLetteringOnFilterEvaluationExceptions
                == y.CreateSubscriptionOptions.EnableDeadLetteringOnFilterEvaluationExceptions
                && string.Equals(x.CreateSubscriptionOptions.ForwardDeadLetteredMessagesTo, y.CreateSubscriptionOptions.ForwardDeadLetteredMessagesTo)
                && string.Equals(x.CreateSubscriptionOptions.ForwardTo, y.CreateSubscriptionOptions.ForwardTo)
                && x.CreateSubscriptionOptions.LockDuration == y.CreateSubscriptionOptions.LockDuration
                && x.CreateSubscriptionOptions.MaxDeliveryCount == y.CreateSubscriptionOptions.MaxDeliveryCount
                && x.CreateSubscriptionOptions.RequiresSession == y.CreateSubscriptionOptions.RequiresSession
                && string.Equals(x.CreateSubscriptionOptions.UserMetadata, y.CreateSubscriptionOptions.UserMetadata);
        }

        public int GetHashCode(SubscriptionEntity obj)
        {
            unchecked
            {
                var hashCode = obj.CreateSubscriptionOptions.SubscriptionName.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.CreateSubscriptionOptions.TopicName.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.CreateSubscriptionOptions.AutoDeleteOnIdle.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.CreateSubscriptionOptions.DefaultMessageTimeToLive.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.CreateSubscriptionOptions.EnableBatchedOperations.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.CreateSubscriptionOptions.DeadLetteringOnMessageExpiration.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.CreateSubscriptionOptions.EnableDeadLetteringOnFilterEvaluationExceptions.GetHashCode();
                if (!string.IsNullOrWhiteSpace(obj.CreateSubscriptionOptions.ForwardDeadLetteredMessagesTo))
                    hashCode = (hashCode * 397) ^ obj.CreateSubscriptionOptions.ForwardDeadLetteredMessagesTo.GetHashCode();

                if (!string.IsNullOrWhiteSpace(obj.CreateSubscriptionOptions.ForwardTo))
                    hashCode = (hashCode * 397) ^ obj.CreateSubscriptionOptions.ForwardTo.GetHashCode();

                hashCode = (hashCode * 397) ^ obj.CreateSubscriptionOptions.LockDuration.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.CreateSubscriptionOptions.MaxDeliveryCount.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.CreateSubscriptionOptions.RequiresSession.GetHashCode();
                if (!string.IsNullOrWhiteSpace(obj.CreateSubscriptionOptions.UserMetadata))
                    hashCode = (hashCode * 397) ^ obj.CreateSubscriptionOptions.UserMetadata.GetHashCode();

                return hashCode;
            }
        }
    }


    sealed class NameEqualityComparer :
        IEqualityComparer<SubscriptionEntity>
    {
        public bool Equals(SubscriptionEntity? x, SubscriptionEntity? y)
        {
            if (ReferenceEquals(x, y))
                return true;

            if (ReferenceEquals(x, null))
                return false;

            if (ReferenceEquals(y, null))
                return false;

            if (x.GetType() != y.GetType())
                return false;

            return string.Equals(x.CreateSubscriptionOptions.SubscriptionName, y.CreateSubscriptionOptions.SubscriptionName)
                && string.Equals(x.CreateSubscriptionOptions.TopicName, y.CreateSubscriptionOptions.TopicName);
        }

        public int GetHashCode(SubscriptionEntity obj)
        {
            var hashCode = obj.CreateSubscriptionOptions.SubscriptionName.GetHashCode();
            hashCode = (hashCode * 397) ^ obj.CreateSubscriptionOptions.TopicName.GetHashCode();

            return hashCode;
        }
    }
}
