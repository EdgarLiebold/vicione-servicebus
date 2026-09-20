using System;
using System.Collections.Generic;
using System.Linq;
using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>Represents an Azure Service Bus topic subscription declaration in broker topology.</summary>
public class SubscriptionEntity :
    Subscription,
    SubscriptionHandle
{
    readonly TopicEntity _topic;
    readonly CreateSubscriptionOptions _createSubscriptionOptions;
    readonly CreateRuleOptions? _rule;
    readonly RuleFilter? _filter;

    /// <summary>Creates a topology subscription for a topic.</summary>
    /// <param name="id">The topology-local identifier.</param>
    /// <param name="topic">The subscribed topic.</param>
    /// <param name="createSubscriptionOptions">The Azure subscription declaration options.</param>
    /// <param name="rule">The optional initial subscription rule.</param>
    /// <param name="filter">The optional broker rule filter.</param>
    public SubscriptionEntity(long id, TopicEntity topic, CreateSubscriptionOptions createSubscriptionOptions, CreateRuleOptions? rule = null,
        RuleFilter? filter = null)
    {
        Id = id;

        _topic = topic;

        _createSubscriptionOptions = Snapshot(createSubscriptionOptions);
        _rule = RuleSnapshot.Copy(rule);
        _filter = RuleSnapshot.Copy(filter);
    }

    /// <summary>Gets a comparer that considers only topic and subscription names.</summary>
    public static IEqualityComparer<SubscriptionEntity> NameComparer { get; } = new NameEqualityComparer();
    /// <summary>Gets a comparer that considers names and subscription declaration properties.</summary>
    public static IEqualityComparer<SubscriptionEntity> EntityComparer { get; } = new SubscriptionEntityEqualityComparer();

    /// <summary>Gets a snapshot of the Azure subscription declaration options.</summary>
    public CreateSubscriptionOptions CreateSubscriptionOptions => Snapshot(_createSubscriptionOptions);

    /// <summary>Gets the subscribed topic.</summary>
    public TopicHandle Topic => _topic;

    /// <summary>Gets a snapshot of the optional initial subscription rule.</summary>
    public CreateRuleOptions? Rule => RuleSnapshot.Copy(_rule);
    /// <summary>Gets a snapshot of the optional broker rule filter.</summary>
    public RuleFilter? Filter => RuleSnapshot.Copy(_filter);
    /// <summary>Gets the topology-local identifier.</summary>
    public long Id { get; }
    /// <summary>Gets this entity through the read-only subscription contract.</summary>
    public Subscription Subscription => this;

    /// <summary>Formats the topic and subscription names for diagnostics.</summary>
    /// <returns>A diagnostic string containing topic and subscription names.</returns>
    public override string ToString()
    {
        return string.Join(", ",
            new[] { $"topic: {_topic.CreateTopicOptions.Name}", $"subscription: {_createSubscriptionOptions.SubscriptionName}" }.Where(x =>
                !string.IsNullOrWhiteSpace(x)));
    }

    internal static CreateSubscriptionOptions Snapshot(CreateSubscriptionOptions source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var copy = new CreateSubscriptionOptions(source.TopicName, source.SubscriptionName)
        {
            AutoDeleteOnIdle = source.AutoDeleteOnIdle,
            DefaultMessageTimeToLive = source.DefaultMessageTimeToLive,
            EnableBatchedOperations = source.EnableBatchedOperations,
            DeadLetteringOnMessageExpiration = source.DeadLetteringOnMessageExpiration,
            EnableDeadLetteringOnFilterEvaluationExceptions = source.EnableDeadLetteringOnFilterEvaluationExceptions,
            ForwardDeadLetteredMessagesTo = source.ForwardDeadLetteredMessagesTo,
            ForwardTo = source.ForwardTo,
            LockDuration = source.LockDuration,
            MaxDeliveryCount = source.MaxDeliveryCount,
            RequiresSession = source.RequiresSession,
            Status = source.Status,
        };

        if (source.UserMetadata is not null)
            copy.UserMetadata = source.UserMetadata;

        return copy;
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

            CreateSubscriptionOptions left = x._createSubscriptionOptions;
            CreateSubscriptionOptions right = y._createSubscriptionOptions;
            return SameIdentityAndLifetime(left, right)
                && SameDelivery(left, right)
                && SameForwarding(left, right)
                && left.Status == right.Status
                && object.Equals(x._rule, y._rule)
                && object.Equals(x._filter, y._filter);
        }

        static bool SameIdentityAndLifetime(CreateSubscriptionOptions x, CreateSubscriptionOptions y) =>
            BrokerName.Equals(x.SubscriptionName, y.SubscriptionName)
            && BrokerName.Equals(x.TopicName, y.TopicName)
            && x.AutoDeleteOnIdle == y.AutoDeleteOnIdle
            && x.DefaultMessageTimeToLive == y.DefaultMessageTimeToLive
            && string.Equals(x.UserMetadata, y.UserMetadata);

        static bool SameDelivery(CreateSubscriptionOptions x, CreateSubscriptionOptions y) =>
            x.EnableBatchedOperations == y.EnableBatchedOperations
            && x.DeadLetteringOnMessageExpiration == y.DeadLetteringOnMessageExpiration
            && x.EnableDeadLetteringOnFilterEvaluationExceptions == y.EnableDeadLetteringOnFilterEvaluationExceptions
            && x.LockDuration == y.LockDuration
            && x.MaxDeliveryCount == y.MaxDeliveryCount
            && x.RequiresSession == y.RequiresSession;

        static bool SameForwarding(CreateSubscriptionOptions x, CreateSubscriptionOptions y) =>
            BrokerName.Equals(x.ForwardDeadLetteredMessagesTo, y.ForwardDeadLetteredMessagesTo)
            && BrokerName.Equals(x.ForwardTo, y.ForwardTo);

        public int GetHashCode(SubscriptionEntity obj)
        {
            return NameEqualityComparer.HashName(obj._createSubscriptionOptions);
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

            return BrokerName.Equals(x._createSubscriptionOptions.SubscriptionName, y._createSubscriptionOptions.SubscriptionName)
                && BrokerName.Equals(x._createSubscriptionOptions.TopicName, y._createSubscriptionOptions.TopicName);
        }

        public int GetHashCode(SubscriptionEntity obj)
        {
            return HashName(obj._createSubscriptionOptions);
        }

        internal static int HashName(CreateSubscriptionOptions options)
        {
            var hashCode = BrokerName.GetHashCode(options.SubscriptionName);
            return (hashCode * 397) ^ BrokerName.GetHashCode(options.TopicName);
        }
    }
}
