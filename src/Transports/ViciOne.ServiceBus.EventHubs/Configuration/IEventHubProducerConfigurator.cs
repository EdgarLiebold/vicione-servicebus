using System;
using Azure.Messaging.EventHubs.Producer;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Configures a producer's send pipeline, serializer, and Azure SDK client options.</summary>
public interface IEventHubProducerConfigurator :
    ISendObserverConnector,
    ISendPipelineConfigurator
{
    /// <summary>Sets the callback applied when the producer client options are created.</summary>
    Action<EventHubProducerClientOptions> ConfigureOptions { set; }

    /// <summary>Sets the outbound message serializer.</summary>
    /// <param name="factory">The factory to create the message serializer.</param>
    /// <param name="isSerializer">Whether this factory becomes the default outbound serializer.</param>
    void AddSerializer(ISerializerFactory factory, bool isSerializer = true);
}
