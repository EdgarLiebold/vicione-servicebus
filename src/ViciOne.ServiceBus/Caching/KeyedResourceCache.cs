#nullable enable
namespace ViciOne.ServiceBus.Caching
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;


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

        public KeyedResourceCache(Func<TValue, TKey> keySelector, ResourceCacheOptions options,
            IEqualityComparer<TKey>? comparer = null)
        {
            ArgumentNullException.ThrowIfNull(keySelector);
            ArgumentNullException.ThrowIfNull(options);

            _cache = new ResourceCache<TValue>(options);
            _index = _cache.AddIndex("primary", keySelector, comparer: comparer);
        }

        public ResourceCacheStatistics Statistics => _cache.Statistics;

        public ValueTask<TValue> GetAsync(TKey key, CancellationToken cancellationToken = default)
        {
            return _index.GetAsync(key, cancellationToken);
        }

        public ValueTask<TValue> GetOrAddAsync(TKey key, ResourceFactory<TKey, TValue> factory, CancellationToken cancellationToken = default)
        {
            return _index.GetOrAddAsync(key, factory, cancellationToken);
        }

        public ValueTask<bool> RemoveAsync(TKey key, CancellationToken cancellationToken = default)
        {
            return _index.RemoveAsync(key, cancellationToken);
        }

        public ValueTask ClearAsync(CancellationToken cancellationToken = default)
        {
            return _cache.ClearAsync(cancellationToken);
        }

        public ValueTask DisposeAsync()
        {
            return _cache.DisposeAsync();
        }
    }
}
