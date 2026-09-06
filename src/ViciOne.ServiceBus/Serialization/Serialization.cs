using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>
/// Provides a serialization implementation.
/// </summary>
public class Serialization :
    ISerialization
{
    readonly IMessageDeserializer _defaultDeserializer;
    readonly IMessageSerializer _defaultSerializer;
    readonly IDictionary<string, IMessageDeserializer> _deserializers;
    readonly IDictionary<string, IMessageSerializer> _serializers;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="serializers">The serializers value.</param>
    /// <param name="serializerContentType">The serializer content type value.</param>
    /// <param name="deserializers">The deserializers value.</param>
    /// <param name="defaultContentType">The default content type value.</param>
    public Serialization(IEnumerable<IMessageSerializer> serializers, ContentType serializerContentType,
        IEnumerable<IMessageDeserializer> deserializers, ContentType defaultContentType)
    {
        if (serializerContentType == null)
            throw new ArgumentNullException(nameof(serializerContentType));
        if (defaultContentType == null)
            throw new ArgumentNullException(nameof(defaultContentType));

        DefaultContentType = defaultContentType;

        _serializers = new Dictionary<string, IMessageSerializer>(StringComparer.OrdinalIgnoreCase);

        foreach (var serializer in serializers)
            _serializers[serializer.ContentType.MediaType] = serializer;

        if (!_serializers.TryGetValue(serializerContentType.MediaType, out var defaultSerializer))
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Serialization", "unknown", $"The serializer content type was not found: {serializerContentType}", "Correct the named configuration before starting the host"));

        _defaultSerializer = defaultSerializer;

        _deserializers = new Dictionary<string, IMessageDeserializer>(StringComparer.OrdinalIgnoreCase);

        foreach (var deserializer in deserializers)
            _deserializers[deserializer.ContentType.MediaType] = deserializer;

        if (!_deserializers.TryGetValue(defaultContentType.MediaType, out var defaultDeserializer))
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Serialization", "unknown", $"The default content type deserializer was not found: {defaultContentType}", "Correct the named configuration before starting the host"));

        _defaultDeserializer = defaultDeserializer;
    }

    /// <summary>
    /// Gets the default content type value.
    /// </summary>
    public ContentType DefaultContentType { get; }

    /// <summary>
    /// Gets message serializer.
    /// </summary>
    /// <param name="contentType">The content type value.</param>
    /// <returns>The result of the operation.</returns>
    public IMessageSerializer GetMessageSerializer(ContentType? contentType = null)
    {
        var mediaType = contentType?.MediaType;

        if (mediaType != null && _serializers.TryGetValue(mediaType, out var serializer))
            return serializer;

        return _defaultSerializer;
    }

    /// <summary>
    /// Attempts to get message serializer.
    /// </summary>
    /// <param name="contentType">The content type value.</param>
    /// <param name="serializer">The serializer value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetMessageSerializer(ContentType contentType, [NotNullWhen(true)] out IMessageSerializer? serializer)
    {
        var mediaType = contentType.MediaType;

        return _serializers.TryGetValue(mediaType, out serializer);
    }

    /// <summary>
    /// Gets message deserializer.
    /// </summary>
    /// <param name="contentType">The content type value.</param>
    /// <returns>The result of the operation.</returns>
    public IMessageDeserializer GetMessageDeserializer(ContentType? contentType = null)
    {
        var mediaType = contentType?.MediaType;

        if (mediaType != null && _deserializers.TryGetValue(mediaType, out var deserializer))
            return deserializer;

        return _defaultDeserializer;
    }

    /// <summary>
    /// Attempts to get message deserializer.
    /// </summary>
    /// <param name="contentType">The content type value.</param>
    /// <param name="deserializer">The deserializer value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetMessageDeserializer(ContentType contentType, [NotNullWhen(true)] out IMessageDeserializer? deserializer)
    {
        var mediaType = contentType.MediaType;

        return _deserializers.TryGetValue(mediaType, out deserializer);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("serializers");
        foreach (var deserializer in _deserializers.Values)
            deserializer.Probe(scope);
    }
}
