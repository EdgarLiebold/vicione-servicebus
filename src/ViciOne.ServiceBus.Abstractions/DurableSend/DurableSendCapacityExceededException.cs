using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Raised when accepting a new durable send would exceed the configured hard retained-record or logical
/// retained-content-byte bound. Quarantined records count against the bounds until explicitly discarded.
/// </summary>
public sealed class DurableSendCapacityExceededException : Exception
{
    public DurableSendCapacityExceededException(string message, int storedCount, long storedBytes)
        : base(message)
    {
        StoredCount = storedCount;
        StoredBytes = storedBytes;
    }

    public int StoredCount { get; }

    public long StoredBytes { get; }
}
