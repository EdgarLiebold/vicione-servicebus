using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Caching;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Provides an event hub producer cache implementation.
/// </summary>
/// <typeparam name="TKey">The t key type.</typeparam>
public class EventHubProducerCache<TKey> :
    IEventHubProducerCache<TKey>
    where TKey : notnull
{
    readonly KeyedResourceCache<TKey, CachedEventHubProducer<TKey>> _cache;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public EventHubProducerCache()
    {
        var options = new ResourceCacheOptions(minAge: TimeSpan.FromSeconds(10));

        _cache = new KeyedResourceCache<TKey, CachedEventHubProducer<TKey>>(x => x.Key, options);
    }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public ValueTask DisposeAsync()
    {
        return _cache.DisposeAsync();
    }

    /// <summary>
    /// Gets producer.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="factory">The factory value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<IEventHubProducer> GetProducerAsync(TKey key, Func<TKey, Task<IEventHubProducer>> factory, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(factory);

        return await _cache.GetOrAddAsync(key,
            async (address, _) => new CachedEventHubProducer<TKey>(address, await factory(address).ConfigureAwait(false)), cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
