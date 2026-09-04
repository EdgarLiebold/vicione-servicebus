using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Caching;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubIntegration;

public class EventHubProducerCache<TKey> :
    IEventHubProducerCache<TKey>
{
    readonly KeyedResourceCache<TKey, CachedEventHubProducer<TKey>> _cache;

    public EventHubProducerCache()
    {
        var options = new ResourceCacheOptions(minAge: TimeSpan.FromSeconds(10));

        _cache = new KeyedResourceCache<TKey, CachedEventHubProducer<TKey>>(x => x.Key, options);
    }

    public ValueTask DisposeAsync()
    {
        return _cache.DisposeAsync();
    }

    public async Task<IEventHubProducer> GetProducer(TKey key, Func<TKey, Task<IEventHubProducer>> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        return await _cache.GetOrAddAsync(key,
            async (address, _) => new CachedEventHubProducer<TKey>(address, await factory(address).ConfigureAwait(false))).ConfigureAwait(false);
    }
}
