using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.EventHubIntegration;

public interface IEventHubProducerCache<TKey> :
    IAsyncDisposable
{
    Task<IEventHubProducer> GetProducer(TKey key, Func<TKey, Task<IEventHubProducer>> factory);
}
