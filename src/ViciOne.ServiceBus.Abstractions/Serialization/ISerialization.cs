using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>
/// Defines the contract for serialization.
/// </summary>
public interface ISerialization :
    IProbeSite
{
    /// <summary>
    /// Gets the default content type value.
    /// </summary>
    ContentType DefaultContentType { get; }

    /// <summary>
    /// Gets message serializer.
    /// </summary>
    /// <param name="contentType">The content type value.</param>
    /// <returns>The result of the operation.</returns>
    IMessageSerializer GetMessageSerializer(ContentType? contentType = null);

    /// <summary>
    /// Attempts to get message serializer.
    /// </summary>
    /// <param name="contentType">The content type value.</param>
    /// <param name="serializer">The serializer value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetMessageSerializer(ContentType contentType, [NotNullWhen(true)] out IMessageSerializer? serializer);

    /// <summary>
    /// Gets message deserializer.
    /// </summary>
    /// <param name="contentType">The content type value.</param>
    /// <returns>The result of the operation.</returns>
    IMessageDeserializer GetMessageDeserializer(ContentType? contentType = null);

    /// <summary>
    /// Attempts to get message deserializer.
    /// </summary>
    /// <param name="contentType">The content type value.</param>
    /// <param name="deserializer">The deserializer value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetMessageDeserializer(ContentType contentType, [NotNullWhen(true)] out IMessageDeserializer? deserializer);
}
