using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Identifies a body that can expose serializer text when its native transport representation contains one.</summary>
public interface TransportTextMessageBody :
    MessageBody
{
    /// <summary>Tries to expose the application payload text after removing any provider envelope.</summary>
    /// <param name="text">The serializer text when the native body contains one.</param>
    /// <returns><see langword="true" /> when serializer text is available; otherwise, <see langword="false" />.</returns>
    bool TryGetPayloadText([NotNullWhen(true)] out string? text);
}
