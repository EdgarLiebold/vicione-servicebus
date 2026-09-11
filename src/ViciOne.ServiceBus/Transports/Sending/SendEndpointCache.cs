using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Caching;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Caches transport send endpoints by their normalized address key.</summary>
/// <typeparam name="TKey">The normalized identity used to cache an endpoint.</typeparam>
internal sealed class SendEndpointCache<TKey> :
    ISendEndpointCache<TKey>
    where TKey : notnull
{
    readonly KeyedResourceCache<TKey, CachedSendEndpoint<TKey>> _cache;

    /// <summary>Initializes a bounded sliding-lifetime endpoint cache.</summary>
    public SendEndpointCache()
    {
        var options = new ResourceCacheOptions(SendEndpointCacheDefaults.Capacity, SendEndpointCacheDefaults.MinAge,
            SendEndpointCacheDefaults.MaxAge, ResourceCacheExpirationMode.Sliding);

        _cache = new KeyedResourceCache<TKey, CachedSendEndpoint<TKey>>(x => x.Key, options);
    }

    /// <summary>Releases every endpoint and transport owned by the cache.</summary>
    /// <returns>A value task that completes after cached resources have been disposed.</returns>
    public ValueTask DisposeAsync()
    {
        return _cache.DisposeAsync();
    }

    /// <inheritdoc />
    public async Task<ISendEndpoint> GetSendEndpointAsync(TKey key, SendEndpointFactory<TKey> factory, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(factory);
        cancellationToken.ThrowIfCancellationRequested();

        CachedSendEndpoint<TKey> sendEndpoint = await _cache.GetOrAddAsync(
            key,
            async (address, creationCancellationToken) =>
            {
                Task<ISendEndpoint> endpointTask = factory(address, creationCancellationToken)
                    ?? throw new InvalidOperationException("The send endpoint factory returned no creation task.");
                ISendEndpoint endpoint = await endpointTask.ConfigureAwait(false)
                    ?? throw new InvalidOperationException("The send endpoint factory returned no endpoint.");

                return new CachedSendEndpoint<TKey>(address, endpoint);
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return sendEndpoint;
    }
}
