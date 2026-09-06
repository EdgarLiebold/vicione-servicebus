using System;

namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>
/// Bounded operational snapshot of durable-send retained state. StoredBytes is logical retained content bytes
/// (serialized body + ServiceBus metadata), not provider physical database allocation.
/// </summary>
/// <param name="StoredCount">The stored count.</param>
/// <param name="StoredBytes">The stored bytes.</param>
/// <param name="PendingCount">The pending count.</param>
/// <param name="RetryScheduledCount">The retry scheduled count.</param>
/// <param name="AwaitingConsumerCompletionCount">The awaiting consumer completion count.</param>
/// <param name="QuarantinedCount">The quarantined count.</param>
/// <param name="OldestPendingEnqueuedAt">The oldest pending enqueued at.</param>
public readonly record struct DurableSendStoreSnapshot(
    int StoredCount,
    long StoredBytes,
    int PendingCount,
    int RetryScheduledCount,
    int AwaitingConsumerCompletionCount,
    int QuarantinedCount,
    DateTimeOffset? OldestPendingEnqueuedAt);
