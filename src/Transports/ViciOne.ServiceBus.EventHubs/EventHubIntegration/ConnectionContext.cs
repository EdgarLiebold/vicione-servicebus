using Azure.Messaging.EventHubs.Producer;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Defines the contract for connection context.
/// </summary>
public interface ConnectionContext :
    PipeContext
{
    /// <summary>
    /// Creates event hub client.
    /// </summary>
    /// <param name="eventHubName">The event hub name value.</param>
    /// <returns>The result of the operation.</returns>
    EventHubProducerClient CreateEventHubClient(string eventHubName);
}
