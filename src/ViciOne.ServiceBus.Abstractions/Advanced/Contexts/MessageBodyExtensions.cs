using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides guarded access to optional message-body representations.</summary>
public static class MessageBodyExtensions
{
    /// <summary>Gets the serializer-defined representation required by a text-only transport.</summary>
    /// <param name="body">The serialized message body.</param>
    /// <returns>The lossless transport-text representation.</returns>
    /// <exception cref="InvalidOperationException">The body does not define a transport-text representation.</exception>
    public static string GetRequiredTransportText(this MessageBody body)
    {
        ArgumentNullException.ThrowIfNull(body);
        if (body.TryGetTransportText(out var text))
            return text;

        throw new InvalidOperationException(
            $"Message body type '{body.GetType().FullName}' does not define the text representation required by this transport.");
    }
}
