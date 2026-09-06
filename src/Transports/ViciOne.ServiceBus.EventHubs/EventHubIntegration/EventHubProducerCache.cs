using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Caching;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Caches Event Hubs producers and disposes entries after resource-cache eviction or shutdown.</summary>
/// <typeparam name="TKey">The producer cache-key type.</typeparam>
public class EventHubProducerCache<TKey> :
    IEventHubProducerCache<TKey>
    where TKey : notnull
{
    readonly KeyedResourceCache<TKey, CachedEventHubProducer<TKey>> _cache;

    /// <summary>Creates a producer cache with a ten-second minimum resource age.</summary>
    public EventHubProducerCache()
    {
        var options = new ResourceCacheOptions(minAge: TimeSpan.FromSeconds(10));

        _cache = new KeyedResourceCache<TKey, CachedEventHubProducer<TKey>>(x => x.Key, options);
    }

    /// <summary>Disposes the resource cache and all producers it owns.</summary>
    /// <returns>A task that completes after cached producers have been disposed.</returns>
    public ValueTask DisposeAsync()
    {
        return _cache.DisposeAsync();
    }

    /// <summary>Gets a cached producer or creates a cache-owned producer for the key.</summary>
    /// <param name="key">The key that identifies the producer.</param>
    /// <param name="factory">Creates the producer when the key is absent.</param>
    /// <param name="cancellationToken">Cancels cache lookup or producer creation.</param>
    /// <returns>A task whose result is the cached or newly created producer.</returns>
    public async Task<IEventHubProducer> GetProducerAsync(TKey key, Func<TKey, Task<IEventHubProducer>> factory, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(factory);

        return await _cache.GetOrAddAsync(key,
            async (address, _) => new CachedEventHubProducer<TKey>(address, await factory(address).ConfigureAwait(false)), cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
