using System;

namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>
/// Hard retained-record and logical retained-content-byte bounds owned by one durable-send store registration.
/// All retained records, including quarantine, count against both applicable bounds. <see cref="MaximumStoredBytes" />
/// is the sum of serialized body and ServiceBus metadata bytes; it is not a provider physical-allocation limit.
/// Physical store quotas/row/index overhead remain provider/host capacity concerns.
/// </summary>
public readonly record struct DurableSendStoreLimits
{
    /// <summary>Creates positive hard limits for retained records and logical content bytes.</summary>
    /// <param name="maximumStoredCount">The maximum number of retained records.</param>
    /// <param name="maximumStoredBytes">The maximum retained serialized-body and metadata bytes.</param>
    /// <exception cref="ArgumentOutOfRangeException">Either limit is less than one.</exception>
    public DurableSendStoreLimits(int maximumStoredCount, long maximumStoredBytes)
    {
        if (maximumStoredCount < 1)
            throw new ArgumentOutOfRangeException(nameof(maximumStoredCount));
        if (maximumStoredBytes < 1)
            throw new ArgumentOutOfRangeException(nameof(maximumStoredBytes));

        MaximumStoredCount = maximumStoredCount;
        MaximumStoredBytes = maximumStoredBytes;
    }

    /// <summary>Gets the maximum number of retained records.</summary>
    public int MaximumStoredCount { get; }

    /// <summary>Gets the maximum logical content bytes retained across all records.</summary>
    public long MaximumStoredBytes { get; }
}
