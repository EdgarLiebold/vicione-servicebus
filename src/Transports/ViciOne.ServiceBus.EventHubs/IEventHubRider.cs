using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Controls the Event Hubs rider and exposes its endpoint and producer facilities.</summary>
public interface IEventHubRider :
    IRiderControl,
    IEventHubEndpointConnector
{
    /// <summary>Gets the rider's producer provider, optionally scoped to a consumed message.</summary>
    /// <param name="consumeContext">The consume context whose headers should flow to produced messages, or <see langword="null" />.</param>
    /// <returns>The unscoped producer provider or a consume-context-aware wrapper.</returns>
    IEventHubProducerProvider GetProducerProvider(ConsumeContext? consumeContext = default);
}
