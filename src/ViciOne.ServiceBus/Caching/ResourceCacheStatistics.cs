namespace ViciOne.ServiceBus.Caching;

/// <summary>Point-in-time cache statistics.</summary>
/// <param name="Count">The number of committed resources.</param>
/// <param name="PendingCreations">The number of cache-owned factories currently in flight.</param>
/// <param name="TotalCreated">The number of resources committed since the cache was created.</param>
/// <param name="Hits">The number of lookups that found a committed or in-flight resource.</param>
/// <param name="Misses">The number of lookups that did not find a resource.</param>
/// <param name="CreationFaults">The number of cache-owned factories that faulted.</param>
/// <param name="Evictions">The number of resources removed by expiration or capacity pressure.</param>
public readonly record struct ResourceCacheStatistics(int Count, int PendingCreations, long TotalCreated, long Hits, long Misses,
    long CreationFaults, long Evictions)
{
    /// <summary>Gets the proportion of recorded lookups that were hits.</summary>
    public double HitRatio => Hits == 0 ? 0 : (double)Hits / ((double)Hits + Misses);
}
