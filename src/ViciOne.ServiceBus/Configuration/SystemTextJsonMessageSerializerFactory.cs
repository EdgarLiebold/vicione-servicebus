using System;
using System.Net.Mime;
using System.Text.Json;
using ViciOne.ServiceBus.Serialization;

#nullable enable
namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a system text json message serializer factory implementation.
/// </summary>
public class SystemTextJsonMessageSerializerFactory :
    ISerializerFactory,
    IJsonSerializerFactory
{
    readonly Lazy<SystemTextJsonMessageSerializer>? _serializer;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public SystemTextJsonMessageSerializerFactory()
    {
    }

    SystemTextJsonMessageSerializerFactory(JsonSerializerOptions options)
    {
        _serializer = new Lazy<SystemTextJsonMessageSerializer>(() => new SystemTextJsonMessageSerializer(options));
    }

    /// <summary>
    /// Gets the content type value.
    /// </summary>
    public ContentType ContentType => SystemTextJsonMessageSerializer.JsonContentType;

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
        return new SystemTextJsonMessageSerializerFactory(options);
    }

    SystemTextJsonMessageSerializer GetSerializer()
    {
        return _serializer?.Value
            ?? throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("System Text Json Message Serializer", "unknown", "The System.Text.Json serializer factory must be bound to a serialization configuration before use.", "Correct the named configuration before starting the host"));
    }
}
