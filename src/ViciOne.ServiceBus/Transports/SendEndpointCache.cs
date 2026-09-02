namespace ViciOne.ServiceBus.Transports
{
    using System;
    using System.Threading.Tasks;
    using Caching;


    /// <summary>
    /// Caches transport send endpoints by their normalized address key.
    /// </summary>
    public class SendEndpointCache<TKey> :
        ISendEndpointCache<TKey>
    {
        readonly KeyedResourceCache<TKey, CachedSendEndpoint<TKey>> _cache;

        public SendEndpointCache()
        {
            var options = new ResourceCacheOptions(SendEndpointCacheDefaults.Capacity, SendEndpointCacheDefaults.MinAge,
                SendEndpointCacheDefaults.MaxAge, ResourceCacheExpirationMode.Sliding);

            _cache = new KeyedResourceCache<TKey, CachedSendEndpoint<TKey>>(x => x.Key, options);
        }

        public async Task<ISendEndpoint> GetSendEndpoint(TKey key, SendEndpointFactory<TKey> factory)
        {
            ArgumentNullException.ThrowIfNull(factory);

            CachedSendEndpoint<TKey> sendEndpoint = await _cache.GetOrAddAsync(key,
                async (address, _) => new CachedSendEndpoint<TKey>(address, await factory(address).ConfigureAwait(false))).ConfigureAwait(false);

            return sendEndpoint;
        }
    }
}
