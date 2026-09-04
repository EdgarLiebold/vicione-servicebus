using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus;

public interface IEventHubRider :
    IRiderControl,
    IEventHubEndpointConnector
{
    IEventHubProducerProvider GetProducerProvider(ConsumeContext consumeContext = default);
}
