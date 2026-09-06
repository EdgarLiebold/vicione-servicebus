using System;
using Azure.Messaging.EventHubs.Producer;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Defines the contract for event hub producer configurator.
/// </summary>
public interface IEventHubProducerConfigurator :
    ISendObserverConnector,
    ISendPipelineConfigurator
{
    /// <summary>
    /// Configure <see cref="EventHubProducerClientOptions" />
    /// </summary>
    Action<EventHubProducerClientOptions> ConfigureOptions { set; }

    /// <summary>
    /// Sets the outbound message serializer
    /// </summary>
    /// <param name="factory">The factory to create the message serializer</param>
    /// <param name="isSerializer"></param>
    void AddSerializer(ISerializerFactory factory, bool isSerializer = true);
}
