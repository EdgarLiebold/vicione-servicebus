using Azure.Messaging.EventHubs.Producer;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Creates producer clients from the configured Event Hubs namespace credentials.</summary>
public interface ConnectionContext :
    PipeContext
{
    /// <summary>Creates an Azure SDK producer client for a named Event Hub.</summary>
    /// <param name="eventHubName">The Event Hub entity name.</param>
    /// <returns>The producer client for the entity.</returns>
    EventHubProducerClient CreateEventHubClient(string eventHubName);
}
