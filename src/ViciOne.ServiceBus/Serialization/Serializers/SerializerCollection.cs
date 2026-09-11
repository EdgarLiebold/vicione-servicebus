using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Resolves configured message serializers and deserializers by media type.</summary>
internal sealed class SerializerCollection :
    ISerialization
{
    readonly IMessageDeserializer _defaultDeserializer;
    readonly IMessageSerializer _defaultSerializer;
    readonly IDictionary<string, IMessageDeserializer> _deserializers;
    readonly IDictionary<string, IMessageSerializer> _serializers;
    readonly string _defaultContentType;

    /// <summary>Creates an immutable serializer lookup from validated configuration.</summary>
    /// <param name="serializers">The serializers available for outgoing messages.</param>
    /// <param name="serializerContentType">The default outgoing media type.</param>
    /// <param name="deserializers">The deserializers available for incoming messages.</param>
    /// <param name="defaultContentType">The fallback incoming media type.</param>
    public SerializerCollection(IEnumerable<IMessageSerializer> serializers, ContentType serializerContentType,
        IEnumerable<IMessageDeserializer> deserializers, ContentType defaultContentType)
    {
        ArgumentNullException.ThrowIfNull(serializers);
        ArgumentNullException.ThrowIfNull(serializerContentType);
        ArgumentNullException.ThrowIfNull(deserializers);
        ArgumentNullException.ThrowIfNull(defaultContentType);

        _defaultContentType = defaultContentType.ToString();

        _serializers = new Dictionary<string, IMessageSerializer>(StringComparer.OrdinalIgnoreCase);

        foreach (var serializer in serializers)
        {
            ArgumentNullException.ThrowIfNull(serializer);
            _serializers[serializer.ContentType.MediaType] = serializer;
        }

        if (!_serializers.TryGetValue(serializerContentType.MediaType, out var defaultSerializer))
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Serialization", "unknown", $"The serializer content type was not found: {serializerContentType}", "Correct the named configuration before starting the host"));

        _defaultSerializer = defaultSerializer;

        _deserializers = new Dictionary<string, IMessageDeserializer>(StringComparer.OrdinalIgnoreCase);

        foreach (var deserializer in deserializers)
        {
            ArgumentNullException.ThrowIfNull(deserializer);
            _deserializers[deserializer.ContentType.MediaType] = deserializer;
        }

        if (!_deserializers.TryGetValue(defaultContentType.MediaType, out var defaultDeserializer))
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Serialization", "unknown", $"The default content type deserializer was not found: {defaultContentType}", "Correct the named configuration before starting the host"));

        _defaultDeserializer = defaultDeserializer;
    }

    /// <summary>Gets the media type used when an incoming transport omits one.</summary>
    public ContentType DefaultContentType => new(_defaultContentType);

    /// <summary>Gets the serializer for a media type or the configured outgoing default.</summary>
    /// <param name="contentType">The requested media type, or <see langword="null" /> for the default.</param>
    /// <returns>The matching serializer or the outgoing default.</returns>
    public IMessageSerializer GetMessageSerializer(ContentType? contentType = null)
    {
        var mediaType = contentType?.MediaType;

        if (mediaType != null && _serializers.TryGetValue(mediaType, out var serializer))
            return serializer;

        return _defaultSerializer;
    }

    /// <summary>Tries to resolve an explicitly registered outgoing serializer.</summary>
    /// <param name="contentType">The requested media type.</param>
    /// <param name="serializer">The matching serializer when registered.</param>
    /// <returns><see langword="true" /> when a matching serializer is registered; otherwise, <see langword="false" />.</returns>
    public bool TryGetMessageSerializer(ContentType contentType, [NotNullWhen(true)] out IMessageSerializer? serializer)
    {
        ArgumentNullException.ThrowIfNull(contentType);
        var mediaType = contentType.MediaType;

        return _serializers.TryGetValue(mediaType, out serializer);
    }

    /// <summary>Gets the deserializer for a media type or the configured incoming fallback.</summary>
    /// <param name="contentType">The requested media type, or <see langword="null" /> for the fallback.</param>
    /// <returns>The matching deserializer or the incoming fallback.</returns>
    public IMessageDeserializer GetMessageDeserializer(ContentType? contentType = null)
    {
        var mediaType = contentType?.MediaType;

        if (mediaType != null && _deserializers.TryGetValue(mediaType, out var deserializer))
            return deserializer;

        return _defaultDeserializer;
    }

    /// <summary>Tries to resolve an explicitly registered incoming deserializer.</summary>
    /// <param name="contentType">The requested media type.</param>
    /// <param name="deserializer">The matching deserializer when registered.</param>
    /// <returns><see langword="true" /> when a matching deserializer is registered; otherwise, <see langword="false" />.</returns>
    public bool TryGetMessageDeserializer(ContentType contentType, [NotNullWhen(true)] out IMessageDeserializer? deserializer)
    {
        ArgumentNullException.ThrowIfNull(contentType);
        var mediaType = contentType.MediaType;

        return _deserializers.TryGetValue(mediaType, out deserializer);
    }

    /// <summary>Adds every registered deserializer to a diagnostic probe.</summary>
    /// <param name="context">The probe to populate.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var scope = context.CreateScope("serializers");
        foreach (var deserializer in _deserializers.Values)
            deserializer.Probe(scope);
    }
}
