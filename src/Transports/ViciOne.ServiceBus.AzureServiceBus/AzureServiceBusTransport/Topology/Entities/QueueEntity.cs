using System;
using System.Collections.Generic;
using System.Linq;
using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>Represents an Azure Service Bus queue declaration in broker topology.</summary>
public class QueueEntity :
    Queue,
    QueueHandle
{
    readonly CreateQueueOptions _createQueueOptions;

    /// <summary>Creates a topology queue from Azure declaration options.</summary>
    /// <param name="id">The topology-local identifier.</param>
    /// <param name="createQueueOptions">The Azure queue declaration options.</param>
    public QueueEntity(long id, CreateQueueOptions createQueueOptions)
    {
        Id = id;
        _createQueueOptions = Snapshot(createQueueOptions);
    }

    /// <summary>Gets a comparer that considers only the Azure queue name.</summary>
    public static IEqualityComparer<QueueEntity> NameComparer { get; } = new NameEqualityComparer();
    /// <summary>Gets a comparer that considers the queue name and declaration properties.</summary>
    public static IEqualityComparer<QueueEntity> EntityComparer { get; } = new QueueEntityEqualityComparer();

    /// <summary>Gets a snapshot of the Azure queue declaration options.</summary>
    public CreateQueueOptions CreateQueueOptions => Snapshot(_createQueueOptions);
    /// <summary>Gets the topology-local identifier.</summary>
    public long Id { get; }
    /// <summary>Gets this entity through the read-only queue contract.</summary>
    public Queue Queue => this;

    /// <summary>Formats the queue path for diagnostics.</summary>
    /// <returns>A diagnostic string containing the queue path.</returns>
    public override string ToString()
    {
        return string.Join(", ", new[] { $"path: {_createQueueOptions.Name}" }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    internal void PromotePartitioning() => _createQueueOptions.EnablePartitioning = true;

    static CreateQueueOptions Snapshot(CreateQueueOptions source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var copy = new CreateQueueOptions(source.Name)
        {
            AutoDeleteOnIdle = source.AutoDeleteOnIdle,
            DefaultMessageTimeToLive = source.DefaultMessageTimeToLive,
            DuplicateDetectionHistoryTimeWindow = source.DuplicateDetectionHistoryTimeWindow,
            EnableBatchedOperations = source.EnableBatchedOperations,
            DeadLetteringOnMessageExpiration = source.DeadLetteringOnMessageExpiration,
            EnablePartitioning = source.EnablePartitioning,
            ForwardDeadLetteredMessagesTo = source.ForwardDeadLetteredMessagesTo,
            ForwardTo = source.ForwardTo,
            LockDuration = source.LockDuration,
            MaxDeliveryCount = source.MaxDeliveryCount,
            MaxSizeInMegabytes = source.MaxSizeInMegabytes,
            MaxMessageSizeInKilobytes = source.MaxMessageSizeInKilobytes,
            RequiresDuplicateDetection = source.RequiresDuplicateDetection,
            RequiresSession = source.RequiresSession,
            Status = source.Status,
        };

        if (source.UserMetadata is not null)
            copy.UserMetadata = source.UserMetadata;

        AuthorizationRuleSnapshot.CopyTo(source.AuthorizationRules, copy.AuthorizationRules);

        return copy;
    }


    sealed class QueueEntityEqualityComparer :
        IEqualityComparer<QueueEntity>
    {
        public bool Equals(QueueEntity? x, QueueEntity? y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (ReferenceEquals(x, null))
                return false;
            if (ReferenceEquals(y, null))
                return false;
            if (x.GetType() != y.GetType())
                return false;
            CreateQueueOptions left = x._createQueueOptions;
            CreateQueueOptions right = y._createQueueOptions;
            return SameIdentityAndLifetime(left, right)
                && SameDelivery(left, right)
                && SameForwarding(left, right)
                && SameLimits(left, right)
                && SameAccess(left, right);
        }

        static bool SameIdentityAndLifetime(CreateQueueOptions x, CreateQueueOptions y) =>
            string.Equals(x.Name, y.Name)
            && x.AutoDeleteOnIdle == y.AutoDeleteOnIdle
            && x.DefaultMessageTimeToLive == y.DefaultMessageTimeToLive
            && x.DuplicateDetectionHistoryTimeWindow == y.DuplicateDetectionHistoryTimeWindow
            && string.Equals(x.UserMetadata, y.UserMetadata);

        static bool SameDelivery(CreateQueueOptions x, CreateQueueOptions y) =>
            x.EnableBatchedOperations == y.EnableBatchedOperations
            && x.DeadLetteringOnMessageExpiration == y.DeadLetteringOnMessageExpiration
            && x.EnablePartitioning == y.EnablePartitioning
            && x.LockDuration == y.LockDuration
            && x.MaxDeliveryCount == y.MaxDeliveryCount
            && x.RequiresDuplicateDetection == y.RequiresDuplicateDetection
            && x.RequiresSession == y.RequiresSession;

        static bool SameForwarding(CreateQueueOptions x, CreateQueueOptions y) =>
            string.Equals(x.ForwardDeadLetteredMessagesTo, y.ForwardDeadLetteredMessagesTo)
            && string.Equals(x.ForwardTo, y.ForwardTo);

        static bool SameLimits(CreateQueueOptions x, CreateQueueOptions y) =>
            x.MaxSizeInMegabytes == y.MaxSizeInMegabytes
            && x.MaxMessageSizeInKilobytes == y.MaxMessageSizeInKilobytes;

        static bool SameAccess(CreateQueueOptions x, CreateQueueOptions y) =>
            x.Status == y.Status
            && Equals(x.AuthorizationRules, y.AuthorizationRules);

        public int GetHashCode(QueueEntity obj)
        {
            // Declaration settings can change during topology building; the queue name remains stable.
            return obj._createQueueOptions.Name.GetHashCode();
        }
    }


    sealed class NameEqualityComparer :
        IEqualityComparer<QueueEntity>
    {
        public bool Equals(QueueEntity? x, QueueEntity? y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (ReferenceEquals(x, null))
                return false;
            if (ReferenceEquals(y, null))
                return false;
            if (x.GetType() != y.GetType())
                return false;
            return string.Equals(x._createQueueOptions.Name, y._createQueueOptions.Name);
        }

        public int GetHashCode(QueueEntity obj)
        {
            return obj._createQueueOptions.Name.GetHashCode();
        }
    }
}
