using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Caching;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Caches transport send endpoints by their normalized address key.</summary>
/// <typeparam name="TKey">The key used for lookup.</typeparam>
public class SendEndpointCache<TKey> :
    ISendEndpointCache<TKey>
    where TKey : notnull
{
    readonly KeyedResourceCache<TKey, CachedSendEndpoint<TKey>> _cache;

    /// <summary>Initializes a new instance.</summary>
    public SendEndpointCache()
    {
        var options = new ResourceCacheOptions(SendEndpointCacheDefaults.Capacity, SendEndpointCacheDefaults.MinAge,
            SendEndpointCacheDefaults.MaxAge, ResourceCacheExpirationMode.Sliding);

        _cache = new KeyedResourceCache<TKey, CachedSendEndpoint<TKey>>(x => x.Key, options);
    }

    /// <summary>Releases the resources owned by this instance.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public ValueTask DisposeAsync()
    {
        return _cache.DisposeAsync();
    }

    /// <summary>Gets send endpoint.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="factory">The factory invoked by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public async Task<ISendEndpoint> GetSendEndpointAsync(TKey key, SendEndpointFactory<TKey> factory, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(factory);

        CachedSendEndpoint<TKey> sendEndpoint = await _cache.GetOrAddAsync(key,
            async (address, _) => new CachedSendEndpoint<TKey>(address, await factory(address).ConfigureAwait(false)), cancellationToken: cancellationToken).ConfigureAwait(false);

        return sendEndpoint;
    }
}
