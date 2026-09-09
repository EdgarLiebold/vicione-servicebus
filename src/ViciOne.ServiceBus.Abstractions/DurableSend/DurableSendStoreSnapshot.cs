using System;

namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>
/// Bounded operational snapshot of durable-send retained state. StoredBytes is logical retained content bytes
/// (serialized body + ServiceBus metadata), not provider physical database allocation.
/// </summary>
/// <param name="StoredCount">The total number of retained durable-send records.</param>
/// <param name="StoredBytes">The total logical content size of the retained records in bytes.</param>
/// <param name="PendingCount">The number of records immediately eligible for delivery.</param>
/// <param name="RetryScheduledCount">The number of records awaiting a scheduled retry.</param>
/// <param name="AwaitingConsumerCompletionCount">The number of volatile deliveries awaiting logical consumer completion.</param>
/// <param name="QuarantinedCount">The number of records requiring an operator decision.</param>
/// <param name="OldestPendingEnqueuedAt">The admission time of the oldest pending record, or <see langword="null"/> when none is pending.</param>
public readonly record struct DurableSendStoreSnapshot(
    int StoredCount,
    long StoredBytes,
    int PendingCount,
    int RetryScheduledCount,
    int AwaitingConsumerCompletionCount,
    int QuarantinedCount,
    DateTimeOffset? OldestPendingEnqueuedAt);
