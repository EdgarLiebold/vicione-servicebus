using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubIntegration.Configuration;

public interface IEventHubReceiveEndpointSpecification :
    IReceiveEndpointObserverConnector,
    ISpecification
{
    /// <summary>
    /// EventHub name
    /// </summary>
    string EndpointName { get; }

    ReceiveEndpoint CreateReceiveEndpoint(IBusInstance busInstance);
}
