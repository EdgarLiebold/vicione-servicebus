using System;
using System.Net.Mime;
using System.Text.Json;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for bus factory configurator.
/// </summary>
/// <typeparam name="TEndpointConfigurator">The t endpoint configurator type.</typeparam>
public interface IBusFactoryConfigurator<out TEndpointConfigurator> :
    IBusFactoryConfigurator,
    IReceiveConfigurator<TEndpointConfigurator>
    where TEndpointConfigurator : IReceiveEndpointConfigurator
{
}


/// <summary>
/// Defines the contract for bus factory configurator.
/// </summary>
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
    /// <summary>
    /// Gets the message topology value.
    /// </summary>
    IMessageTopologyConfigurator MessageTopology { get; }
    /// <summary>
    /// Gets the consume topology value.
    /// </summary>
    IConsumeTopologyConfigurator ConsumeTopology { get; }
    /// <summary>
    /// Gets the send topology value.
    /// </summary>
    ISendTopologyConfigurator SendTopology { get; }
    /// <summary>
    /// Gets the publish topology value.
    /// </summary>
    IPublishTopologyConfigurator PublishTopology { get; }

    /// <summary>
    /// Set to true if the topology should be deployed only
    /// </summary>
    bool DeployTopologyOnly { set; }

    /// <summary>
    /// Deploys defined Publish message types to the broker at startup
    /// </summary>
    bool DeployPublishTopology { set; }

    /// <summary>
    /// Specify the number of messages to prefetch from the message broker
    /// </summary>
    /// <value>The limit</value>
    int PrefetchCount { set; }

    /// <summary>
    /// Specify the number of concurrent messages that can be consumed (separate from prefetch count)
    /// </summary>
    int? ConcurrentMessageLimit { set; }

    /// <summary>
    /// When deserializing a message, if no ContentType is present on the receive context, use this as the default
    /// </summary>
    ContentType DefaultContentType { set; }

    /// <summary>
    /// When serializing a message, use the content type specified for serialization
    /// </summary>
    ContentType SerializerContentType { set; }

    /// <summary>
    /// Configure the message topology for the message type on this bus configuration
    /// </summary>
    /// <param name="configureTopology"></param>
    /// <typeparam name="T"></typeparam>
    void Message<T>(Action<IMessageTopologyConfigurator<T>> configureTopology)
        where T : class;

    /// <summary>
    /// Configure the send topology of the message type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="configureTopology"></param>
    void Send<T>(Action<IMessageSendTopologyConfigurator<T>> configureTopology)
        where T : class;

    /// <summary>
    /// Configure the send topology of the message type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="configureTopology"></param>
    void Publish<T>(Action<IMessagePublishTopologyConfigurator<T>> configureTopology)
        where T : class;

    /// <summary>
    /// Maps a message type to a destination for this bus only. Routes are frozen when the bus is built.
    /// </summary>
    void Route<T>(Uri destinationAddress)
        where T : class;

    /// <summary>
    /// Maps a message type to a lazily resolved destination for this bus only.
    /// </summary>
    void Route<T>(EndpointAddressProvider<T> endpointAddressProvider)
        where T : class;

    /// <summary>
    /// Add a message serializer using the specified factory (can be shared by serializer/deserializer)
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="isSerializer">If true, set the current serializer to the specified factory</param>
    void AddSerializer(ISerializerFactory factory, bool isSerializer = true);

    /// <summary>
    /// Add a message deserializer using the specified factory (can be shared by serializer/deserializer)
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="isDefault">If true, set the default content type to the content type of the deserializer</param>
    void AddDeserializer(ISerializerFactory factory, bool isDefault = false);

    /// <summary>
    /// Configures the System.Text.Json payload policy for this bus. The configuration is materialized into an immutable runtime snapshot.
    /// </summary>
    void ConfigureSystemTextJsonSerializerOptions(Func<JsonSerializerOptions, JsonSerializerOptions> configure);

    /// <summary>
    /// Clears all message serialization configuration
    /// </summary>
    void ClearSerialization();
}
