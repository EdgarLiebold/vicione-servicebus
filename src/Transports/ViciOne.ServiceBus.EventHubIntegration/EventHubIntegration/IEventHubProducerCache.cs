using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.EventHubIntegration;

public interface IEventHubProducerCache<TKey> :
    IAsyncDisposable
    where TKey : notnull
{
    Task<IEventHubProducer> GetProducerAsync(TKey key, Func<TKey, Task<IEventHubProducer>> factory, CancellationToken cancellationToken = default);
}
