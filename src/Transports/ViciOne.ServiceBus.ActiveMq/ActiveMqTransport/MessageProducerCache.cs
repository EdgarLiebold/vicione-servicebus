using System;
using System.Threading.Tasks;
using Apache.NMS;
using ViciOne.ServiceBus.Caching;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Caches Apache NMS message producers by destination and disposes them on shutdown.</summary>
public class MessageProducerCache :
    Agent
{
    /// <summary>Creates a message producer for a native destination.</summary>
    /// <param name="destination">The producer destination.</param>
    /// <returns>A task that produces the native message producer.</returns>
    public delegate Task<IMessageProducer> MessageProducerFactory(IDestination destination);


    readonly KeyedResourceCache<IDestination, CachedMessageProducer> _cache;

    /// <summary>Creates a producer cache with a ten-second minimum resource age.</summary>
    public MessageProducerCache()
    {
        var options = new ResourceCacheOptions(minAge: TimeSpan.FromSeconds(10));

        _cache = new KeyedResourceCache<IDestination, CachedMessageProducer>(x => x.Destination, options);
    }

    /// <summary>Gets an existing cached producer or creates one for a destination.</summary>
    /// <param name="key">The native destination used as the cache key.</param>
    /// <param name="factory">The asynchronous producer factory.</param>
    /// <param name="cancellationToken">Cancels this caller's cache lookup or wait without canceling shared creation.</param>
    /// <returns>A task that produces the cached native message producer.</returns>
    public async Task<IMessageProducer> GetMessageProducerAsync(IDestination key, MessageProducerFactory factory, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(factory);

        return await GetMessageProducerWithCancellationAsync(key, (destination, _) => factory(destination), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Gets or creates a producer using cache-owned cancellation for shared creation.</summary>
    /// <param name="key">The native destination used as the cache key.</param>
    /// <param name="factory">The producer factory, given the token owned by the shared cache creation.</param>
    /// <param name="cancellationToken">Cancels this caller's lookup or wait without canceling another caller's creation.</param>
    /// <returns>A task that produces the cached native message producer.</returns>
    internal async Task<IMessageProducer> GetMessageProducerWithCancellationAsync(IDestination key,
        Func<IDestination, CancellationToken, Task<IMessageProducer>> factory, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(factory);

        return await _cache.GetOrAddAsync(key,
            async (destination, creationToken) => new CachedMessageProducer(destination,
                await factory(destination, creationToken).ConfigureAwait(false)), cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Disposes all cached message producers when the cache agent stops.</summary>
    /// <param name="context">The agent stop context.</param>
    /// <returns>A task that completes when cache disposal has finished.</returns>
    protected override async Task StopAgentAsync(StopContext context)
    {
        await _cache.DisposeAsync().ConfigureAwait(false);
    }
}
