using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubIntegration.Configuration;

public interface IEventHubProducerSpecification :
    ISpecification
{
    EventHubSendTransportContext CreateSendTransportContext(string eventHubName, IBusInstance busInstance);
}
