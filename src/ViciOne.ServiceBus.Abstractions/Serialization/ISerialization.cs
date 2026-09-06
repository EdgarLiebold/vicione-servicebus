using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Defines the operations required by serialization.</summary>
public interface ISerialization :
    IProbeSite
{
    /// <summary>Gets the default content type.</summary>
    ContentType DefaultContentType { get; }

    /// <summary>Gets message serializer.</summary>
    /// <param name="contentType">The runtime content type used by the operation.</param>
    /// <returns>The message serializer.</returns>
    IMessageSerializer GetMessageSerializer(ContentType? contentType = null);

    /// <summary>Attempts to get message serializer.</summary>
    /// <param name="contentType">The runtime content type used by the operation.</param>
    /// <param name="serializer">Receives the serializer produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetMessageSerializer(ContentType contentType, [NotNullWhen(true)] out IMessageSerializer? serializer);

    /// <summary>Gets message deserializer.</summary>
    /// <param name="contentType">The runtime content type used by the operation.</param>
    /// <returns>The message deserializer.</returns>
    IMessageDeserializer GetMessageDeserializer(ContentType? contentType = null);

    /// <summary>Attempts to get message deserializer.</summary>
    /// <param name="contentType">The runtime content type used by the operation.</param>
    /// <param name="deserializer">Receives the deserializer produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetMessageDeserializer(ContentType contentType, [NotNullWhen(true)] out IMessageDeserializer? deserializer);
}
