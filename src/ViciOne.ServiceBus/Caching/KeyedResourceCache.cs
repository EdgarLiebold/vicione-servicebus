using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Caching;

/// <summary>
/// Convenience facade over the single resource-cache engine for the common one-key transport cache case.
/// It does not own a second cache implementation.
/// </summary>
public sealed class KeyedResourceCache<TKey, TValue> :
    IAsyncDisposable
    where TKey : notnull
    where TValue : class
{
    readonly ResourceCache<TValue> _cache;
    readonly IResourceCacheIndex<TKey, TValue> _index;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="keySelector">The key selector value.</param>
    /// <param name="options">The options value.</param>
    /// <param name="comparer">The comparer value.</param>
    public KeyedResourceCache(Func<TValue, TKey> keySelector, ResourceCacheOptions options,
        IEqualityComparer<TKey>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentNullException.ThrowIfNull(options);

        _cache = new ResourceCache<TValue>(options);
        _index = _cache.AddIndex("primary", keySelector, comparer: comparer);
    }

    /// <summary>
    /// Gets the statistics value.
    /// </summary>
    public ResourceCacheStatistics Statistics => _cache.Statistics;

    /// <summary>
    /// Performs the get operation.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public ValueTask<TValue> GetAsync(TKey key, CancellationToken cancellationToken = default)
    {
        return _index.GetAsync(key, cancellationToken);
    }

    /// <summary>
    /// Gets or add.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="factory">The factory value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public ValueTask<TValue> GetOrAddAsync(TKey key, ResourceFactory<TKey, TValue> factory, CancellationToken cancellationToken = default)
    {
        return _index.GetOrAddAsync(key, factory, cancellationToken);
    }

    /// <summary>
    /// Performs the remove operation.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public ValueTask<bool> RemoveAsync(TKey key, CancellationToken cancellationToken = default)
    {
        return _index.RemoveAsync(key, cancellationToken);
    }

    /// <summary>
    /// Performs the clear operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        return _cache.ClearAsync(cancellationToken);
    }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public ValueTask DisposeAsync()
    {
        return _cache.DisposeAsync();
    }
}
