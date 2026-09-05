using System;
using System.Net.Mime;
using System.Text.Json;
using ViciOne.ServiceBus.Serialization;

#nullable enable
namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a system text json raw message serializer factory implementation.
/// </summary>
public class SystemTextJsonRawMessageSerializerFactory :
    ISerializerFactory,
    IJsonSerializerFactory
{
    readonly RawSerializerOptions _rawOptions;
    readonly Lazy<SystemTextJsonRawMessageSerializer>? _serializer;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="options">The options value.</param>
    public SystemTextJsonRawMessageSerializerFactory(RawSerializerOptions options = RawSerializerOptions.Default)
    {
        _rawOptions = options;
    }

    SystemTextJsonRawMessageSerializerFactory(RawSerializerOptions rawOptions, JsonSerializerOptions options)
    {
        _rawOptions = rawOptions;
        _serializer = new Lazy<SystemTextJsonRawMessageSerializer>(() => new SystemTextJsonRawMessageSerializer(options, rawOptions));
    }

    /// <summary>
    /// Gets the content type value.
    /// </summary>
    public ContentType ContentType => SystemTextJsonRawMessageSerializer.JsonContentType;

    /// <summary>
    /// Creates serializer.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IMessageSerializer CreateSerializer()
    {
        return GetSerializer();
    }

    /// <summary>
    /// Creates deserializer.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IMessageDeserializer CreateDeserializer()
    {
        return GetSerializer();
    }

    ISerializerFactory IJsonSerializerFactory.Bind(JsonSerializerOptions options)
    {
        return new SystemTextJsonRawMessageSerializerFactory(_rawOptions, options);
    }

    SystemTextJsonRawMessageSerializer GetSerializer()
    {
        return _serializer?.Value
            ?? throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("System Text Json Raw Message Serializer", "unknown", "The raw System.Text.Json serializer factory must be bound to a serialization configuration before use.", "Correct the named configuration before starting the host"));
    }
}
