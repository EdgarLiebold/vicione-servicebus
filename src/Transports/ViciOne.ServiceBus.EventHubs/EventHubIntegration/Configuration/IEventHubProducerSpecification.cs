using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs.Configuration;

/// <summary>Validates producer configuration and builds Event Hubs send transport contexts.</summary>
public interface IEventHubProducerSpecification :
    ISpecification
{
    /// <summary>Creates a send transport context for a named Event Hub.</summary>
    /// <param name="eventHubName">The Event Hub entity name.</param>
    /// <param name="busInstance">The bus instance supplying host and topology services.</param>
    /// <returns>The configured Event Hubs send transport context.</returns>
    EventHubSendTransportContext CreateSendTransportContext(string eventHubName, IBusInstance busInstance);
}
