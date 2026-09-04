using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Caching;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Caches transport send endpoints by their normalized address key.
/// </summary>
public class SendEndpointCache<TKey> :
    ISendEndpointCache<TKey>
    where TKey : notnull
{
    readonly KeyedResourceCache<TKey, CachedSendEndpoint<TKey>> _cache;

    public SendEndpointCache()
    {
        var options = new ResourceCacheOptions(SendEndpointCacheDefaults.Capacity, SendEndpointCacheDefaults.MinAge,
            SendEndpointCacheDefaults.MaxAge, ResourceCacheExpirationMode.Sliding);

        _cache = new KeyedResourceCache<TKey, CachedSendEndpoint<TKey>>(x => x.Key, options);
    }

    public ValueTask DisposeAsync()
    {
        return _cache.DisposeAsync();
    }

    public async Task<ISendEndpoint> GetSendEndpointAsync(TKey key, SendEndpointFactory<TKey> factory, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(factory);

        CachedSendEndpoint<TKey> sendEndpoint = await _cache.GetOrAddAsync(key,
            async (address, _) => new CachedSendEndpoint<TKey>(address, await factory(address).ConfigureAwait(false)), cancellationToken: cancellationToken).ConfigureAwait(false);

        return sendEndpoint;
    }
}
