// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.EventHubIntegration.Configuration
{
    using Transports;


    public interface IEventHubProducerSpecification :
        ISpecification
    {
        EventHubSendTransportContext CreateSendTransportContext(string eventHubName, IBusInstance busInstance);
    }
}
