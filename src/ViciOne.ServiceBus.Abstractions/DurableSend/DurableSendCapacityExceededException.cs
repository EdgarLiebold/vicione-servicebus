using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Raised when accepting a new durable send would exceed the configured hard retained-record or logical
/// retained-content-byte bound. Quarantined records count against the bounds until explicitly discarded.
/// </summary>
public sealed class DurableSendCapacityExceededException : Exception
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="storedCount">The stored count value.</param>
    /// <param name="storedBytes">The stored bytes value.</param>
    public DurableSendCapacityExceededException(string message, int storedCount, long storedBytes)
        : base(message)
    {
        StoredCount = storedCount;
        StoredBytes = storedBytes;
    }

    /// <summary>
    /// Gets the stored count value.
    /// </summary>
    public int StoredCount { get; }

    /// <summary>
    /// Gets the stored bytes value.
    /// </summary>
    public long StoredBytes { get; }
}
