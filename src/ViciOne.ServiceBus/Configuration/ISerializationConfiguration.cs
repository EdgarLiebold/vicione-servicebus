using System;
using System.Net.Mime;
using System.Text.Json;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for serialization configuration.
/// </summary>
public interface ISerializationConfiguration :
    ISpecification
{
    /// <summary>
    /// When deserializing a message, if no ContentType is present on the receive context, use this as the default
    /// </summary>
    ContentType DefaultContentType { set; }

    /// <summary>
    /// When serializing a message, the content type of the serializer to use
    /// </summary>
    ContentType SerializerContentType { set; }

    /// <summary>
    /// Adds serializer to the configuration.
    /// </summary>
    /// <param name="factory">The factory value.</param>
    /// <param name="isSerializer">The is serializer value.</param>
    void AddSerializer(ISerializerFactory factory, bool isSerializer = true);

    /// <summary>
    /// Adds deserializer to the configuration.
    /// </summary>
    /// <param name="factory">The factory value.</param>
    /// <param name="isDefault">The is default value.</param>
    void AddDeserializer(ISerializerFactory factory, bool isDefault = false);

    /// <summary>
    /// Adds a local System.Text.Json configuration transform. The effective options are materialized as an immutable
    /// snapshot for this bus or receive endpoint when the serializer collection is built.
    /// </summary>
    void ConfigureSystemTextJsonSerializerOptions(Func<JsonSerializerOptions, JsonSerializerOptions> configure);

    /// <summary>
    /// Clear the configuration, removing all deserializers, serializers, and breaking the
    /// linkage to the bus serialization configuration.
    /// </summary>
    void Clear();

    /// <summary>
    /// Creates serialization configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    ISerializationConfiguration CreateSerializationConfiguration();

    /// <summary>
    /// Compiles the configured serializers into a collection for use by the receive endpoint
    /// </summary>
    /// <returns></returns>
    ISerialization CreateSerializerCollection();
}
