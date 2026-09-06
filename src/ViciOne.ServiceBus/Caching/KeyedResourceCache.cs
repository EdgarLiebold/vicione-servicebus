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
/// <typeparam name="TValue">The value stored by the member.</typeparam>
public sealed class KeyedResourceCache<TKey, TValue> :
    IAsyncDisposable
    where TKey : notnull
    where TValue : class
{
    readonly ResourceCache<TValue> _cache;
    readonly IResourceCacheIndex<TKey, TValue> _index;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="keySelector">The key selector.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <param name="comparer">The comparer.</param>
    public KeyedResourceCache(Func<TValue, TKey> keySelector, ResourceCacheOptions options,
        IEqualityComparer<TKey>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentNullException.ThrowIfNull(options);

        _cache = new ResourceCache<TValue>(options);
        _index = _cache.AddIndex("primary", keySelector, comparer: comparer);
    }

    /// <summary>Gets the statistics.</summary>
    public ResourceCacheStatistics Statistics => _cache.Statistics;

    /// <summary>Retrieves the requested value.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public ValueTask<TValue> GetAsync(TKey key, CancellationToken cancellationToken = default)
    {
        return _index.GetAsync(key, cancellationToken);
    }

    /// <summary>Gets or add.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="factory">The factory invoked by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public ValueTask<TValue> GetOrAddAsync(TKey key, ResourceFactory<TKey, TValue> factory, CancellationToken cancellationToken = default)
    {
        return _index.GetOrAddAsync(key, factory, cancellationToken);
    }

    /// <summary>Removes the selected value.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the remove outcome.</returns>
    public ValueTask<bool> RemoveAsync(TKey key, CancellationToken cancellationToken = default)
    {
        return _index.RemoveAsync(key, cancellationToken);
    }

    /// <summary>Removes every item from the current collection.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
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
