namespace ViciOne.ServiceBus.Caching;

/// <summary>Point-in-time cache statistics.</summary>
/// <param name="Count">The count.</param>
/// <param name="PendingCreations">The pending creations.</param>
/// <param name="TotalCreated">The total created.</param>
/// <param name="Hits">The hits.</param>
/// <param name="Misses">The misses.</param>
/// <param name="CreationFaults">The creation faults.</param>
/// <param name="Evictions">The evictions.</param>
public readonly record struct ResourceCacheStatistics(int Count, int PendingCreations, long TotalCreated, long Hits, long Misses,
    long CreationFaults, long Evictions)
{
    /// <summary>Gets the hit ratio.</summary>
    public double HitRatio => Hits + Misses == 0 ? 0 : (double)Hits / (Hits + Misses);
}
