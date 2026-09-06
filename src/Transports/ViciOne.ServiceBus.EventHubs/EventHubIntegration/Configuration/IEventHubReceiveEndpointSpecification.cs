using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs.Configuration;

/// <summary>Validates and builds an Event Hubs receive endpoint.</summary>
public interface IEventHubReceiveEndpointSpecification :
    IReceiveEndpointObserverConnector,
    ISpecification
{
    /// <summary>Gets the bus endpoint name derived from the Event Hub and consumer group.</summary>
    string EndpointName { get; }

    /// <summary>Creates the configured receive endpoint.</summary>
    /// <param name="busInstance">The bus instance that will own the endpoint.</param>
    /// <returns>The Event Hubs receive endpoint.</returns>
    ReceiveEndpoint CreateReceiveEndpoint(IBusInstance busInstance);
}
