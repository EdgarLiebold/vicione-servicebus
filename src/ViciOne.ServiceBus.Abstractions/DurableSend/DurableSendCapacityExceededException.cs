using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Raised when accepting a new durable send would exceed the configured hard retained-record or logical
/// retained-content-byte bound. Quarantined records count against the bounds until explicitly discarded.
/// </summary>
public sealed class DurableSendCapacityExceededException : ViciOneServiceBusException
{
    /// <summary>Creates a capacity failure with the retained-store totals observed at admission.</summary>
    /// <param name="message">The capacity failure description.</param>
    /// <param name="storedCount">The number of records retained when admission was rejected.</param>
    /// <param name="storedBytes">The logical content bytes retained when admission was rejected.</param>
    /// <exception cref="ArgumentOutOfRangeException">Either retained-store total is negative.</exception>
    public DurableSendCapacityExceededException(string message, int storedCount, long storedBytes)
        : base(message)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(storedCount);
        ArgumentOutOfRangeException.ThrowIfNegative(storedBytes);

        StoredCount = storedCount;
        StoredBytes = storedBytes;
    }

    /// <summary>Gets the retained-record count observed when admission was rejected.</summary>
    public int StoredCount { get; }

    /// <summary>Gets the retained logical content bytes observed when admission was rejected.</summary>
    public long StoredBytes { get; }
}
