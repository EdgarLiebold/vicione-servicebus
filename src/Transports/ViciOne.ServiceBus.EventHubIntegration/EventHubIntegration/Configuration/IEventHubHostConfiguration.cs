// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.EventHubIntegration.Configuration
{
    using System;
    using Transports;


    public interface IEventHubHostConfiguration :
        ISpecification
    {
        IConnectionContextSupervisor ConnectionContextSupervisor { get; }

        EventHubSendTransportContext CreateSendTransportContext(string eventHubName, IBusInstance busInstance);

        IEventHubReceiveEndpointSpecification CreateSpecification(string eventHubName, string consumerGroup,
            Action<IEventHubReceiveEndpointConfigurator> configure);

        IEventHubRider Build(IRiderRegistrationContext context, IBusInstance busInstance);
    }
}
