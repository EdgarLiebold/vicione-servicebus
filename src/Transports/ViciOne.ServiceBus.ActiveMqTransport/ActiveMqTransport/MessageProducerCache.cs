using System;
using System.Threading.Tasks;
using Apache.NMS;
using ViciOne.ServiceBus.Caching;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMqTransport;

public class MessageProducerCache :
    Agent
{
    public delegate Task<IMessageProducer> MessageProducerFactory(IDestination destination);


    readonly KeyedResourceCache<IDestination, CachedMessageProducer> _cache;

    public MessageProducerCache()
    {
        var options = new ResourceCacheOptions(minAge: TimeSpan.FromSeconds(10));

        _cache = new KeyedResourceCache<IDestination, CachedMessageProducer>(x => x.Destination, options);
    }

    public async Task<IMessageProducer> GetMessageProducer(IDestination key, MessageProducerFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        return await _cache.GetOrAddAsync(key,
            async (destination, _) => new CachedMessageProducer(destination, await factory(destination).ConfigureAwait(false))).ConfigureAwait(false);
    }

    protected override async Task StopAgent(StopContext context)
    {
        await _cache.DisposeAsync().ConfigureAwait(false);
    }
}
