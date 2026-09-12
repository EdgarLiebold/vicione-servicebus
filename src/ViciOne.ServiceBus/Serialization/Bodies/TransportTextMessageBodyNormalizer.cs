using System;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Converts an admitted text-native transport body into the selected serializer's body representation.</summary>
internal static class TransportTextMessageBodyNormalizer
{
    /// <summary>Normalizes serializer text while leaving binary and bodyless transport representations unchanged.</summary>
    /// <param name="body">The admitted native transport body.</param>
    /// <param name="deserializer">The deserializer selected by the message content type.</param>
    /// <returns>The serializer-specific body, or the original body when no payload text is available.</returns>
    internal static MessageBody Normalize(MessageBody body, IMessageDeserializer deserializer)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(deserializer);

        if (body is not TransportTextMessageBody textBody || !textBody.TryGetPayloadText(out var text))
            return body;

        return deserializer.GetMessageBody(text)
            ?? throw new InvalidOperationException("The selected deserializer returned no message body for the transport text payload.");
    }
}
