using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

public interface IEventHubProducerProvider :
    ISendObserverConnector
{
    Task<IEventHubProducer> GetProducerAsync(Uri address, CancellationToken cancellationToken = default);
}
