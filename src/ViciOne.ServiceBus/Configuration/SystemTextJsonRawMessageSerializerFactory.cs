using System;
using System.Net.Mime;
using System.Text.Json;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Creates system text json raw message serializer instances.</summary>
public class SystemTextJsonRawMessageSerializerFactory :
    ISerializerFactory,
    IJsonSerializerFactory
{
    readonly RawSerializerOptions _rawOptions;
    readonly Lazy<SystemTextJsonRawMessageSerializer>? _serializer;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="options">The options that control the operation.</param>
    public SystemTextJsonRawMessageSerializerFactory(RawSerializerOptions options = RawSerializerOptions.Default)
    {
        _rawOptions = options;
    }

    SystemTextJsonRawMessageSerializerFactory(RawSerializerOptions rawOptions, JsonSerializerOptions options)
    {
        _rawOptions = rawOptions;
        _serializer = new Lazy<SystemTextJsonRawMessageSerializer>(() => new SystemTextJsonRawMessageSerializer(options, rawOptions));
    }

    /// <summary>Gets the content type.</summary>
    public ContentType ContentType => SystemTextJsonRawMessageSerializer.JsonContentType;

    /// <summary>Creates serializer.</summary>
    /// <returns>The created serializer.</returns>
    public IMessageSerializer CreateSerializer()
    {
        return GetSerializer();
    }

    /// <summary>Creates deserializer.</summary>
    /// <returns>The created deserializer.</returns>
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
