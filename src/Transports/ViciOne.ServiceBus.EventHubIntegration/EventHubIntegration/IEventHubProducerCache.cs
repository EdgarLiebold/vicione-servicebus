namespace ViciOne.ServiceBus.EventHubIntegration
{
    using System;
    using System.Threading.Tasks;


    public interface IEventHubProducerCache<TKey> :
        IAsyncDisposable
    {
        Task<IEventHubProducer> GetProducer(TKey key, Func<TKey, Task<IEventHubProducer>> factory);
    }
}
