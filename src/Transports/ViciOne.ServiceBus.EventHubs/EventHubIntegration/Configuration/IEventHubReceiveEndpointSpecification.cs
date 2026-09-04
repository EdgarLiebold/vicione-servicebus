using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs.Configuration;

/// <summary>
/// Defines the contract for event hub receive endpoint specification.
/// </summary>
public interface IEventHubReceiveEndpointSpecification :
    IReceiveEndpointObserverConnector,
    ISpecification
{
    /// <summary>
    /// EventHub name
    /// </summary>
    string EndpointName { get; }

    /// <summary>
    /// Creates receive endpoint.
    /// </summary>
    /// <param name="busInstance">The bus instance value.</param>
    /// <returns>The result of the operation.</returns>
    ReceiveEndpoint CreateReceiveEndpoint(IBusInstance busInstance);
}
