using System;

namespace ViciOne.ServiceBus.Caching;

/// <summary>
/// Implemented by resources that can report real use independently of cache lookups.
/// Sliding lifetime caches subscribe while the resource is owned by the cache.
/// </summary>
public interface IResourceUsageSource
{
    /// <summary>Occurs when used.</summary>
    event Action? Used;
}
