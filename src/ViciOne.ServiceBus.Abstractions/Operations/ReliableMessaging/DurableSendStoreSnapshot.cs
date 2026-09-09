using System;

namespace ViciOne.ServiceBus.Operations;

/// <summary>
/// Bounded operational snapshot of durable-send retained state. <c>StoredBytes</c> is logical serialized-body and
/// ServiceBus metadata content, not provider physical database allocation.
/// </summary>
public readonly record struct DurableSendStoreSnapshot
{
    /// <summary>Creates a consistent aggregate snapshot of all retained durable-send states.</summary>
    /// <param name="storedCount">The total number of retained records.</param>
    /// <param name="storedBytes">The total logical content bytes retained by those records.</param>
    /// <param name="pendingCount">The number of nonterminal records, including retry and consumer-completion waits.</param>
    /// <param name="retryScheduledCount">The pending records waiting for a scheduled retry.</param>
    /// <param name="awaitingConsumerCompletionCount">The pending volatile deliveries awaiting logical completion.</param>
    /// <param name="quarantinedCount">The terminal records requiring an operator decision.</param>
    /// <param name="oldestPendingEnqueuedAt">The oldest admission time among pending records, or <see langword="null" /> when none exists.</param>
    /// <exception cref="ArgumentException">The aggregate state counts are inconsistent.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A count or byte total is negative.</exception>
    public DurableSendStoreSnapshot(
        int storedCount,
        long storedBytes,
        int pendingCount,
        int retryScheduledCount,
        int awaitingConsumerCompletionCount,
        int quarantinedCount,
        DateTimeOffset? oldestPendingEnqueuedAt)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(storedCount);
        ArgumentOutOfRangeException.ThrowIfNegative(storedBytes);
        ArgumentOutOfRangeException.ThrowIfNegative(pendingCount);
        ArgumentOutOfRangeException.ThrowIfNegative(retryScheduledCount);
        ArgumentOutOfRangeException.ThrowIfNegative(awaitingConsumerCompletionCount);
        ArgumentOutOfRangeException.ThrowIfNegative(quarantinedCount);
        if ((long)pendingCount + quarantinedCount != storedCount)
            throw new ArgumentException("Pending and quarantined counts must equal the retained-record count.");
        if ((long)retryScheduledCount + awaitingConsumerCompletionCount > pendingCount)
            throw new ArgumentException("Pending substate counts cannot exceed the pending-record count.");
        if ((pendingCount == 0) != (oldestPendingEnqueuedAt is null))
            throw new ArgumentException("The oldest pending timestamp must exist exactly when pending records exist.", nameof(oldestPendingEnqueuedAt));

        StoredCount = storedCount;
        StoredBytes = storedBytes;
        PendingCount = pendingCount;
        RetryScheduledCount = retryScheduledCount;
        AwaitingConsumerCompletionCount = awaitingConsumerCompletionCount;
        QuarantinedCount = quarantinedCount;
        OldestPendingEnqueuedAt = oldestPendingEnqueuedAt;
    }

    /// <summary>Gets the total number of retained records.</summary>
    public int StoredCount { get; }

    /// <summary>Gets the total logical content bytes retained by all records.</summary>
    public long StoredBytes { get; }

    /// <summary>Gets the number of nonterminal records.</summary>
    public int PendingCount { get; }

    /// <summary>Gets the pending records waiting for a scheduled retry.</summary>
    public int RetryScheduledCount { get; }

    /// <summary>Gets the pending volatile deliveries awaiting logical consumer completion.</summary>
    public int AwaitingConsumerCompletionCount { get; }

    /// <summary>Gets the terminal records requiring an operator decision.</summary>
    public int QuarantinedCount { get; }

    /// <summary>Gets the oldest pending admission time, or <see langword="null" /> when no pending record exists.</summary>
    public DateTimeOffset? OldestPendingEnqueuedAt { get; }
}
