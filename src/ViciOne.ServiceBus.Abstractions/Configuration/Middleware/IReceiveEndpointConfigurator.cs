using System;
using System.Net.Mime;
using System.Text.Json;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configure a receiving endpoint.</summary>
public interface IReceiveEndpointConfigurator :
    IEndpointConfigurator,
    IReceiveEndpointObserverConnector,
    IReceiveEndpointDependencyConnector,
    IReceiveEndpointDependentConnector
{
    /// <summary>Returns the input address of the receive endpoint.</summary>
    Uri InputAddress { get; }

    /// <summary>
    /// If <see langword="true" /> (the default), configures the provider-specific receive topology for
    /// message types consumed by handlers, consumers, sagas, and activities on this endpoint.
    /// </summary>
    bool ConfigureConsumeTopology { set; }

    /// <summary>If true (the default), faults should be published when no ResponseAddress or FaultAddress are present.</summary>
    bool PublishFaults { set; }

    /// <summary>Specify the number of messages to prefetch from the message broker.</summary>
    /// <value>The limit</value>
    int PrefetchCount { get; set; }

    /// <summary>Specify the number of concurrent messages that can be consumed (separate from prefetch count).</summary>
    int? ConcurrentMessageLimit { get; set; }

    /// <summary>When deserializing a message, if no ContentType is present on the receive context, use this as the default.</summary>
    ContentType DefaultContentType { set; }

    /// <summary>When serializing a message, use the content type specified for serialization.</summary>
    ContentType SerializerContentType { set; }

    /// <summary>
    /// Configures whether the broker topology is configured for the specified message type. Related to
    /// <see cref="ConfigureConsumeTopology" />, but for an individual message type.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="enabled">The enabled.</param>
    void ConfigureMessageTopology<T>(bool enabled = true)
        where T : class;

    /// <summary>
    /// Configures whether the broker topology is configured for the specified message type. Related to
    /// <see cref="ConfigureConsumeTopology" />, but for an individual message type.
    /// </summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="enabled">The enabled.</param>
    void ConfigureMessageTopology(Type messageType, bool enabled = true);

    /// <summary>Adds endpoint specification to the configuration.</summary>
    /// <param name="configurator">The configurator to update.</param>
    void AddEndpointSpecification(IReceiveEndpointSpecification configurator);

    /// <summary>Add a message serializer using the specified factory (can be shared by serializer/deserializer).</summary>
    /// <param name="factory">The factory invoked by the operation.</param>
    /// <param name="isSerializer">If true, set the current serializer to the specified factory.</param>
    void AddSerializer(ISerializerFactory factory, bool isSerializer = true);

    /// <summary>Add a message deserializer using the specified factory (can be shared by serializer/deserializer).</summary>
    /// <param name="factory">The factory invoked by the operation.</param>
    /// <param name="isDefault">If true, set the default content type to the content type of the deserializer.</param>
    void AddDeserializer(ISerializerFactory factory, bool isDefault = false);

    /// <summary>Configures the System.Text.Json payload policy for this receive endpoint. Endpoint configuration is isolated from the parent bus.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    void ConfigureSystemTextJsonSerializerOptions(Func<JsonSerializerOptions, JsonSerializerOptions> configure);

    /// <summary>Clears all message serialization configuration.</summary>
    void ClearSerialization();
}
