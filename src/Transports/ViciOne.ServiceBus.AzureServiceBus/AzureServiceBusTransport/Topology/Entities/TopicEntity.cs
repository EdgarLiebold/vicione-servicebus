using System;
using System.Collections.Generic;
using System.Linq;
using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>Represents an Azure Service Bus topic declaration in broker topology.</summary>
public class TopicEntity :
    Topic,
    TopicHandle
{
    readonly CreateTopicOptions _createTopicOptions;

    /// <summary>Creates a topology topic from Azure declaration options.</summary>
    /// <param name="id">The topology-local identifier.</param>
    /// <param name="createTopicOptions">The Azure topic declaration options.</param>
    public TopicEntity(long id, CreateTopicOptions createTopicOptions)
    {
        Id = id;
        _createTopicOptions = Snapshot(createTopicOptions);
    }

    /// <summary>Gets a comparer that considers only the Azure topic name.</summary>
    public static IEqualityComparer<TopicEntity> NameComparer { get; } = new NameEqualityComparer();
    /// <summary>Gets a comparer that considers the topic name and declaration properties.</summary>
    public static IEqualityComparer<TopicEntity> EntityComparer { get; } = new TopicEntityEqualityComparer();

    /// <summary>Gets a snapshot of the Azure topic declaration options.</summary>
    public CreateTopicOptions CreateTopicOptions => Snapshot(_createTopicOptions);
    /// <summary>Gets the topology-local identifier.</summary>
    public long Id { get; }
    /// <summary>Gets this entity through the read-only topic contract.</summary>
    public Topic Topic => this;

    /// <summary>Formats the topic path for diagnostics.</summary>
    /// <returns>A diagnostic string containing the topic path.</returns>
    public override string ToString()
    {
        return string.Join(", ", new[] { $"path: {_createTopicOptions.Name}" }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    internal bool IsPartitioned => _createTopicOptions.EnablePartitioning;

    internal void PromotePartitioning() => _createTopicOptions.EnablePartitioning = true;

    static CreateTopicOptions Snapshot(CreateTopicOptions source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var copy = new CreateTopicOptions(source.Name)
        {
            AutoDeleteOnIdle = source.AutoDeleteOnIdle,
            DefaultMessageTimeToLive = source.DefaultMessageTimeToLive,
            DuplicateDetectionHistoryTimeWindow = source.DuplicateDetectionHistoryTimeWindow,
            EnableBatchedOperations = source.EnableBatchedOperations,
            EnablePartitioning = source.EnablePartitioning,
            MaxSizeInMegabytes = source.MaxSizeInMegabytes,
            MaxMessageSizeInKilobytes = source.MaxMessageSizeInKilobytes,
            RequiresDuplicateDetection = source.RequiresDuplicateDetection,
            Status = source.Status,
            SupportOrdering = source.SupportOrdering,
        };

        if (source.UserMetadata is not null)
            copy.UserMetadata = source.UserMetadata;

        AuthorizationRuleSnapshot.CopyTo(source.AuthorizationRules, copy.AuthorizationRules);

        return copy;
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
            CreateTopicOptions left = x._createTopicOptions;
            CreateTopicOptions right = y._createTopicOptions;
            return SameIdentityAndLifetime(left, right)
                && SameDelivery(left, right)
                && SameLimits(left, right)
                && SameAccess(left, right);
        }

        static bool SameIdentityAndLifetime(CreateTopicOptions x, CreateTopicOptions y) =>
            string.Equals(x.Name, y.Name)
            && x.AutoDeleteOnIdle == y.AutoDeleteOnIdle
            && x.DefaultMessageTimeToLive == y.DefaultMessageTimeToLive
            && x.DuplicateDetectionHistoryTimeWindow == y.DuplicateDetectionHistoryTimeWindow
            && string.Equals(x.UserMetadata, y.UserMetadata);

        static bool SameDelivery(CreateTopicOptions x, CreateTopicOptions y) =>
            x.EnableBatchedOperations == y.EnableBatchedOperations
            && x.EnablePartitioning == y.EnablePartitioning
            && x.RequiresDuplicateDetection == y.RequiresDuplicateDetection
            && x.SupportOrdering == y.SupportOrdering;

        static bool SameLimits(CreateTopicOptions x, CreateTopicOptions y) =>
            x.MaxSizeInMegabytes == y.MaxSizeInMegabytes
            && x.MaxMessageSizeInKilobytes == y.MaxMessageSizeInKilobytes;

        static bool SameAccess(CreateTopicOptions x, CreateTopicOptions y) =>
            x.Status == y.Status
            && Equals(x.AuthorizationRules, y.AuthorizationRules);

        public int GetHashCode(TopicEntity obj)
        {
            // Declaration settings can change during topology building; the topic name remains stable.
            return obj._createTopicOptions.Name.GetHashCode();
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
            return string.Equals(x._createTopicOptions.Name, y._createTopicOptions.Name);
        }

        public int GetHashCode(TopicEntity obj)
        {
            return obj._createTopicOptions.Name.GetHashCode();
        }
    }
}
