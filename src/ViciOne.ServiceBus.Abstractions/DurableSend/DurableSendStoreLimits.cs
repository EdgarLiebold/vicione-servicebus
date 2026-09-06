using System;

namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>
/// Hard retained-record and logical retained-content-byte bounds owned by one durable-send store registration.
/// All retained records, including quarantine, count against both applicable bounds. MaximumStoredBytes is the sum of
/// serialized body + ServiceBus metadata bytes; it is not a claim about a provider's physical database allocation.
/// Physical store quotas/row/index overhead remain provider/host capacity concerns.
/// </summary>
public readonly record struct DurableSendStoreLimits
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="maximumStoredCount">The maximum stored count.</param>
    /// <param name="maximumStoredBytes">The maximum stored bytes.</param>
    public DurableSendStoreLimits(int maximumStoredCount, long maximumStoredBytes)
    {
        if (maximumStoredCount < 1)
            throw new ArgumentOutOfRangeException(nameof(maximumStoredCount));
        if (maximumStoredBytes < 1)
            throw new ArgumentOutOfRangeException(nameof(maximumStoredBytes));

        MaximumStoredCount = maximumStoredCount;
        MaximumStoredBytes = maximumStoredBytes;
    }

    /// <summary>Gets the maximum stored count.</summary>
    public int MaximumStoredCount { get; }

    /// <summary>Maximum logical retained content bytes (serialized body + ServiceBus metadata) across retained records.</summary>
    public long MaximumStoredBytes { get; }
}
