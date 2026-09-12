using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides isolated access to one complete serialized message body.</summary>
public interface MessageBody
{
    /// <summary>Gets the serialized content length in bytes.</summary>
    long Length { get; }

    /// <summary>Copies the complete serialized content into a new array.</summary>
    /// <returns>An independently mutable copy of the serialized content.</returns>
    byte[] ToArray();

    /// <summary>Opens a new read-only stream over the serialized content.</summary>
    /// <returns>An independently disposable stream positioned at the beginning of the body.</returns>
    Stream OpenReadStream();

    /// <summary>Tries to get the serializer-defined representation used by text-only transports.</summary>
    /// <param name="text">The lossless transport text when this body defines one.</param>
    /// <returns><see langword="true" /> when a transport-text representation is available; otherwise, <see langword="false" />.</returns>
    bool TryGetTransportText([NotNullWhen(true)] out string? text);
}
