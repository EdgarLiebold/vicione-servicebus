#nullable enable
namespace ViciOne.ServiceBus.Caching;

/// <summary>
/// Point-in-time cache statistics.
/// </summary>
public readonly record struct ResourceCacheStatistics(int Count, int PendingCreations, long TotalCreated, long Hits, long Misses,
    long CreationFaults, long Evictions)
{
    public double HitRatio => Hits + Misses == 0 ? 0 : (double)Hits / (Hits + Misses);
}
