namespace ViciOne.ServiceBus.ActiveMqTransport
{
    using System;
    using System.Threading.Tasks;
    using Apache.NMS;
    using Caching;
    using ViciOne.ServiceBus.Middleware;
    using Transports;


    public class MessageProducerCache :
        Agent
    {
        public delegate Task<IMessageProducer> MessageProducerFactory(IDestination destination);


        readonly KeyedResourceCache<IDestination, CachedMessageProducer> _cache;

        public MessageProducerCache()
        {
            var options = new ResourceCacheOptions(SendEndpointCacheDefaults.Capacity, SendEndpointCacheDefaults.MinAge,
                SendEndpointCacheDefaults.MaxAge, ResourceCacheExpirationMode.Sliding);

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
            await _cache.ClearAsync(context.CancellationToken).ConfigureAwait(false);
        }
    }
}
