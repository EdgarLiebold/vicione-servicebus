using System;
using System.Net.Mime;
using System.Text.Json;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures transport, topology, serialization, middleware, and lifecycle dependencies for a receive endpoint.</summary>
public interface IReceiveEndpointConfigurator :
    IEndpointConfigurator,
    IReceiveEndpointObserverConnector,
    IReceiveEndpointDependencyConnector,
    IReceiveEndpointDependentConnector
{
    /// <summary>Gets the receive endpoint's input address.</summary>
    Uri InputAddress { get; }

    /// <summary>
    /// If <see langword="true" /> (the default), configures the provider-specific receive topology for
    /// message types consumed by handlers, consumers, sagas, and activities on this endpoint.
    /// </summary>
    bool ConfigureConsumeTopology { set; }

    /// <summary>Sets whether unaddressed consumer faults are published.</summary>
    bool PublishFaults { set; }

    /// <summary>Gets or sets the broker-specific number of messages fetched ahead of processing.</summary>
    int PrefetchCount { get; set; }

    /// <summary>Gets or sets the maximum number of messages processed concurrently on the endpoint.</summary>
    int? ConcurrentMessageLimit { get; set; }

    /// <summary>Sets the content type used when an incoming transport message does not specify one.</summary>
    ContentType DefaultContentType { set; }

    /// <summary>Sets the content type used to serialize messages sent from this endpoint.</summary>
    ContentType SerializerContentType { set; }

    /// <summary>
    /// Configures whether the broker topology is configured for the specified message type. Related to
    /// <see cref="ConfigureConsumeTopology" />, but for an individual message type.
    /// </summary>
    /// <typeparam name="TMessage">The message contract.</typeparam>
    /// <param name="enabled">Whether the transport creates consume topology for the message contract.</param>
    void ConfigureMessageTopology<TMessage>(bool enabled = true)
        where TMessage : class;

    /// <summary>
    /// Configures whether the broker topology is configured for the specified message type. Related to
    /// <see cref="ConfigureConsumeTopology" />, but for an individual message type.
    /// </summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="enabled">Whether the transport creates consume topology for the message contract.</param>
    void ConfigureMessageTopology(Type messageType, bool enabled = true);

    /// <summary>Adds a transport-specific receive-endpoint specification.</summary>
    /// <param name="specification">The endpoint specification to add.</param>
    void AddEndpointSpecification(IReceiveEndpointSpecification specification);

    /// <summary>Adds a message serializer created by the supplied factory.</summary>
    /// <param name="factory">The serializer factory to add.</param>
    /// <param name="isSerializer">Whether this factory becomes the endpoint's serializer.</param>
    void AddSerializer(ISerializerFactory factory, bool isSerializer = true);

    /// <summary>Adds a message deserializer created by the supplied factory.</summary>
    /// <param name="factory">The deserializer factory to add.</param>
    /// <param name="isDefault">Whether this factory's content type becomes the endpoint default.</param>
    void AddDeserializer(ISerializerFactory factory, bool isDefault = false);

    /// <summary>Configures an endpoint-local copy of the System.Text.Json serializer options.</summary>
    /// <param name="configure">The function that returns the configured serializer options.</param>
    void ConfigureSystemTextJsonSerializerOptions(Func<JsonSerializerOptions, JsonSerializerOptions> configure);

    /// <summary>Clears all message serialization configuration.</summary>
    void ClearSerialization();
}
