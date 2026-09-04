using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs.Configuration;

/// <summary>
/// Defines the contract for event hub producer specification.
/// </summary>
public interface IEventHubProducerSpecification :
    ISpecification
{
    /// <summary>
    /// Creates send transport context.
    /// </summary>
    /// <param name="eventHubName">The event hub name value.</param>
    /// <param name="busInstance">The bus instance value.</param>
    /// <returns>The result of the operation.</returns>
    EventHubSendTransportContext CreateSendTransportContext(string eventHubName, IBusInstance busInstance);
}
