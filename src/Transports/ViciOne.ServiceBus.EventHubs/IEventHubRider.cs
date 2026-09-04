using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Defines the contract for event hub rider.
/// </summary>
public interface IEventHubRider :
    IRiderControl,
    IEventHubEndpointConnector
{
    /// <summary>
    /// Gets producer provider.
    /// </summary>
    /// <param name="consumeContext">The consume context value.</param>
    /// <returns>The result of the operation.</returns>
    IEventHubProducerProvider GetProducerProvider(ConsumeContext? consumeContext = default);
}
