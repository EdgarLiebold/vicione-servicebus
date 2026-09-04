using System;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubIntegration.Configuration;

public interface IEventHubHostConfiguration :
    ISpecification
{
    IConnectionContextSupervisor ConnectionContextSupervisor { get; }

    EventHubSendTransportContext CreateSendTransportContext(string eventHubName, IBusInstance busInstance);

    IEventHubReceiveEndpointSpecification CreateSpecification(string eventHubName, string consumerGroup,
        Action<IEventHubReceiveEndpointConfigurator> configure);

    IEventHubRider Build(IRiderRegistrationContext context, IBusInstance busInstance);
}
