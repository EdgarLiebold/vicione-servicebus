using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

public interface IEventHubProducerProvider :
    ISendObserverConnector
{
    Task<IEventHubProducer> GetProducer(Uri address);
}
