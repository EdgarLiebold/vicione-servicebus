using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Bounded operational snapshot of durable-send retained state. StoredBytes is logical retained content bytes
/// (serialized body + ServiceBus metadata), not provider physical database allocation.
/// </summary>
public readonly record struct DurableSendStoreSnapshot(
    int StoredCount,
    long StoredBytes,
    int PendingCount,
    int RetryScheduledCount,
    int AwaitingConsumerCompletionCount,
    int QuarantinedCount,
    DateTimeOffset? OldestPendingEnqueuedAt);
