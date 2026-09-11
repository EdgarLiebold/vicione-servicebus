using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Resolves registered message serializers and deserializers by media content type.</summary>
public interface ISerialization :
    IProbeSite
{
    /// <summary>Gets the media content type used when an operation does not select one.</summary>
    ContentType DefaultContentType { get; }

    /// <summary>Gets the serializer registered for a content type.</summary>
    /// <param name="contentType">The requested content type, or <see langword="null" /> to use <see cref="DefaultContentType" />.</param>
    /// <returns>The serializer selected for the requested or default content type.</returns>
    IMessageSerializer GetMessageSerializer(ContentType? contentType = null);

    /// <summary>Tries to get the serializer registered for a content type.</summary>
    /// <param name="contentType">The requested content type.</param>
    /// <param name="serializer">The registered serializer when found; otherwise, <see langword="null" />.</param>
    /// <returns><see langword="true" /> when a matching serializer is registered; otherwise, <see langword="false" />.</returns>
    bool TryGetMessageSerializer(ContentType contentType, [NotNullWhen(true)] out IMessageSerializer? serializer);

    /// <summary>Gets the deserializer registered for a content type.</summary>
    /// <param name="contentType">The requested content type, or <see langword="null" /> to use <see cref="DefaultContentType" />.</param>
    /// <returns>The deserializer selected for the requested or default content type.</returns>
    IMessageDeserializer GetMessageDeserializer(ContentType? contentType = null);

    /// <summary>Tries to get the deserializer registered for a content type.</summary>
    /// <param name="contentType">The requested content type.</param>
    /// <param name="deserializer">The registered deserializer when found; otherwise, <see langword="null" />.</param>
    /// <returns><see langword="true" /> when a matching deserializer is registered; otherwise, <see langword="false" />.</returns>
    bool TryGetMessageDeserializer(ContentType contentType, [NotNullWhen(true)] out IMessageDeserializer? deserializer);
}
