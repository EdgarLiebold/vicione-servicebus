using System;
using System.Threading.Tasks;
using Apache.NMS;
using ViciOne.ServiceBus.Caching;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Provides a message producer cache implementation.
/// </summary>
public class MessageProducerCache :
    Agent
{
    /// <summary>
    /// Represents the method that handles message producer factory.
    /// </summary>
    /// <param name="destination">The destination value.</param>
    /// <returns>The result of the operation.</returns>
    public delegate Task<IMessageProducer> MessageProducerFactory(IDestination destination);


    readonly KeyedResourceCache<IDestination, CachedMessageProducer> _cache;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public MessageProducerCache()
    {
        var options = new ResourceCacheOptions(minAge: TimeSpan.FromSeconds(10));

        _cache = new KeyedResourceCache<IDestination, CachedMessageProducer>(x => x.Destination, options);
    }

    /// <summary>
    /// Gets message producer.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="factory">The factory value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<IMessageProducer> GetMessageProducerAsync(IDestination key, MessageProducerFactory factory, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(factory);

        return await _cache.GetOrAddAsync(key,
            async (destination, _) => new CachedMessageProducer(destination, await factory(destination).ConfigureAwait(false)), cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Stops agent.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    protected override async Task StopAgentAsync(StopContext context)
    {
        await _cache.DisposeAsync().ConfigureAwait(false);
    }
}
