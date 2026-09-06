using System;
using System.Net.Mime;
using System.Text.Json;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures a bus and exposes transport-specific receive-endpoint settings.</summary>
/// <typeparam name="TEndpointConfigurator">The transport-specific receive-endpoint configurator.</typeparam>
public interface IBusFactoryConfigurator<out TEndpointConfigurator> :
    IBusFactoryConfigurator,
    IReceiveConfigurator<TEndpointConfigurator>
    where TEndpointConfigurator : IReceiveEndpointConfigurator
{
}


/// <summary>Defines the transport-independent configuration of a bus instance and its message pipelines.</summary>
public interface IBusFactoryConfigurator :
    IReceiveConfigurator,
    IConsumePipeConfigurator,
    ISendPipelineConfigurator,
    IPublishPipelineConfigurator,
    IBusObserverConnector,
    IReceiveObserverConnector,
    IConsumeObserverConnector,
    ISendObserverConnector,
    IPublishObserverConnector
{
    /// <summary>Gets the conventions that describe message contracts.</summary>
    IMessageTopologyConfigurator MessageTopology { get; }
    /// <summary>Gets the topology applied when messages are consumed.</summary>
    IConsumeTopologyConfigurator ConsumeTopology { get; }
    /// <summary>Gets the topology applied when messages are sent.</summary>
    ISendTopologyConfigurator SendTopology { get; }
    /// <summary>Gets the topology applied when messages are published.</summary>
    IPublishTopologyConfigurator PublishTopology { get; }

    /// <summary>Sets whether startup deploys topology without starting message delivery.</summary>
    bool DeployTopologyOnly { set; }

    /// <summary>Sets whether startup deploys publish topology for configured message contracts.</summary>
    bool DeployPublishTopology { set; }

    /// <summary>Sets the maximum number of messages prefetched from the broker.</summary>
    int PrefetchCount { set; }

    /// <summary>Sets the maximum number of messages processed concurrently, independently of prefetching.</summary>
    int? ConcurrentMessageLimit { set; }

    /// <summary>Sets the content type used to deserialize messages that do not declare one.</summary>
    ContentType DefaultContentType { set; }

    /// <summary>Sets the content type used to serialize outgoing messages.</summary>
    ContentType SerializerContentType { set; }

    /// <summary>Configures message-topology conventions for a contract.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="configureTopology">The callback that configures the contract topology.</param>
    void Message<T>(Action<IMessageTopologyConfigurator<T>> configureTopology)
        where T : class;

    /// <summary>Configures send topology for a message contract.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="configureTopology">The callback that configures send topology.</param>
    void Send<T>(Action<IMessageSendTopologyConfigurator<T>> configureTopology)
        where T : class;

    /// <summary>Configures publish topology for a message contract.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="configureTopology">The callback that configures publish topology.</param>
    void Publish<T>(Action<IMessagePublishTopologyConfigurator<T>> configureTopology)
        where T : class;

    /// <summary>Maps a message contract to a fixed destination on this bus.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="destinationAddress">The destination to use for the contract.</param>
    void Route<T>(Uri destinationAddress)
        where T : class;

    /// <summary>Maps a message contract to a destination resolved for each send on this bus.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="endpointAddressProvider">The function that resolves the current destination, or <see langword="null" /> when none is available.</param>
    void Route<T>(EndpointAddressProvider endpointAddressProvider)
        where T : class;

    /// <summary>Adds a message serializer to the bus.</summary>
    /// <param name="factory">The factory that creates serializer contexts.</param>
    /// <param name="isSerializer">Whether this serializer becomes the active serializer.</param>
    void AddSerializer(ISerializerFactory factory, bool isSerializer = true);

    /// <summary>Adds a message deserializer to the bus.</summary>
    /// <param name="factory">The factory that creates deserializer contexts.</param>
    /// <param name="isDefault">Whether the deserializer's content type becomes the default.</param>
    void AddDeserializer(ISerializerFactory factory, bool isDefault = false);

    /// <summary>Configures the immutable <see cref="JsonSerializerOptions" /> snapshot used by this bus.</summary>
    /// <param name="configure">The function that transforms the serializer options.</param>
    void ConfigureSystemTextJsonSerializerOptions(Func<JsonSerializerOptions, JsonSerializerOptions> configure);

    /// <summary>Removes every configured serializer and deserializer from the bus.</summary>
    void ClearSerialization();
}
