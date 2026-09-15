using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Caching;

/// <summary>
/// Owns a bounded resource cache accessed through one unique primary key.
/// Resource creation, expiration and disposal use the shared resource-cache engine.
/// </summary>
/// <typeparam name="TKey">The key used for lookup.</typeparam>
/// <typeparam name="TValue">The cache-owned resource type.</typeparam>
public sealed class KeyedResourceCache<TKey, TValue> :
    IAsyncDisposable
    where TKey : notnull
    where TValue : class
{
    readonly ResourceCache<TValue> _cache;
    readonly IResourceCacheIndex<TKey, TValue> _index;

    /// <summary>Initializes a cache with one unique primary index.</summary>
    /// <param name="keySelector">The function that projects the primary key from a resource.</param>
    /// <param name="options">The cache capacity, expiration and lifetime policy.</param>
    /// <param name="comparer">The optional equality comparer for primary keys.</param>
    public KeyedResourceCache(Func<TValue, TKey> keySelector, ResourceCacheOptions options,
        IEqualityComparer<TKey>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentNullException.ThrowIfNull(options);

        _cache = new ResourceCache<TValue>(options);
        _index = _cache.AddIndex("primary", keySelector, comparer: comparer);
    }

    /// <summary>Gets a point-in-time snapshot of cache statistics.</summary>
    public ResourceCacheStatistics Statistics => _cache.Statistics;

    /// <summary>Retrieves a committed resource or waits for its pending creation.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="cancellationToken">The token checked before lookup and used to cancel this caller's pending-resource wait.</param>
    /// <returns>A value task that yields the resource, or fails if the key is absent.</returns>
    public ValueTask<TValue> GetAsync(TKey key, CancellationToken cancellationToken = default)
    {
        return _index.GetAsync(key, cancellationToken);
    }

    /// <summary>Gets a resource or shares one cache-owned creation for the requested key.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="factory">The factory used only when no resource or pending creation exists for the key.</param>
    /// <param name="cancellationToken">The token that cancels this caller's capacity or resource wait without canceling shared creation.</param>
    /// <returns>A value task that yields the resource; a creation owner also awaits its added notifications.</returns>
    public ValueTask<TValue> GetOrAddAsync(TKey key, ResourceFactory<TKey, TValue> factory, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(factory);
        return _index.GetOrAddAsync(key, factory, cancellationToken);
    }

    /// <summary>Removes and releases the committed resource for the key; a pending creation is left unchanged.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="cancellationToken">The token checked before removal starts; it does not cancel committed resource release.</param>
    /// <returns>A value task that yields true only when a committed resource was removed.</returns>
    public ValueTask<bool> RemoveAsync(TKey key, CancellationToken cancellationToken = default)
    {
        return _index.RemoveAsync(key, cancellationToken);
    }

    /// <summary>Removes committed resources and invalidates pending creations, awaiting their ownership release.</summary>
    /// <param name="cancellationToken">The token checked before clearing starts; committed cleanup is not canceled by this token.</param>
    /// <returns>A value task that completes after resource release and clear notifications.</returns>
    public ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        return _cache.ClearAsync(cancellationToken);
    }

    /// <summary>Stops admission, cancels cache-owned creation and awaits all operations and resource release.</summary>
    /// <returns>A value task that observes the shared disposal outcome.</returns>
    public ValueTask DisposeAsync()
    {
        return _cache.DisposeAsync();
    }
}
