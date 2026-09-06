using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Caching;

/// <summary>
/// Convenience facade over the single resource-cache engine for the common one-key transport cache case.
/// It does not own a second cache implementation.
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

    /// <summary>Retrieves the requested value.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="cancellationToken">The token that cancels the lookup or pending-resource wait.</param>
    /// <returns>A task that produces the requested value.</returns>
    public ValueTask<TValue> GetAsync(TKey key, CancellationToken cancellationToken = default)
    {
        return _index.GetAsync(key, cancellationToken);
    }

    /// <summary>Gets an existing resource or creates and caches one for the key.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="factory">The factory invoked by the operation.</param>
    /// <param name="cancellationToken">The token that cancels lookup, admission or pending-resource waits before commit.</param>
    /// <returns>A task that produces the requested value.</returns>
    public ValueTask<TValue> GetOrAddAsync(TKey key, ResourceFactory<TKey, TValue> factory, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(factory);
        return _index.GetOrAddAsync(key, factory, cancellationToken);
    }

    /// <summary>Removes the selected value.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="cancellationToken">The token that cancels removal before it is committed.</param>
    /// <returns>A task that produces the remove outcome.</returns>
    public ValueTask<bool> RemoveAsync(TKey key, CancellationToken cancellationToken = default)
    {
        return _index.RemoveAsync(key, cancellationToken);
    }

    /// <summary>Removes and releases every resource owned by the cache.</summary>
    /// <param name="cancellationToken">The token that cancels clearing before removal is committed.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        return _cache.ClearAsync(cancellationToken);
    }

    /// <summary>Releases the resources owned by this instance.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public ValueTask DisposeAsync()
    {
        return _cache.DisposeAsync();
    }
}
