namespace ViciOne.ServiceBus.EventHubIntegration
{
    using System;
    using System.Threading.Tasks;
    using Caching;
    using Transports;


    public class EventHubProducerCache<TKey> :
        IEventHubProducerCache<TKey>
    {
        readonly KeyedResourceCache<TKey, CachedEventHubProducer<TKey>> _cache;

        public EventHubProducerCache()
        {
            var options = new ResourceCacheOptions(SendEndpointCacheDefaults.Capacity, SendEndpointCacheDefaults.MinAge,
                SendEndpointCacheDefaults.MaxAge, ResourceCacheExpirationMode.Sliding);

            _cache = new KeyedResourceCache<TKey, CachedEventHubProducer<TKey>>(x => x.Key, options);
        }

        public async Task<IEventHubProducer> GetProducer(TKey key, Func<TKey, Task<IEventHubProducer>> factory)
        {
            ArgumentNullException.ThrowIfNull(factory);

            return await _cache.GetOrAddAsync(key,
                async (address, _) => new CachedEventHubProducer<TKey>(address, await factory(address).ConfigureAwait(false))).ConfigureAwait(false);
        }
    }
}
