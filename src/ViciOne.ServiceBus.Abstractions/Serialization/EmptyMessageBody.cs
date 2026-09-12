using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Represents a message body with no content.</summary>
public sealed class EmptyMessageBody :
    MessageBody
{
    /// <summary>Gets the shared empty message body.</summary>
    public static EmptyMessageBody Instance { get; } = new();

    private EmptyMessageBody()
    {
    }

    /// <summary>Gets the zero-byte body length.</summary>
    public long Length => 0;

    /// <summary>Returns an empty byte array.</summary>
    /// <returns>The shared zero-length array.</returns>
    public byte[] ToArray() => [];

    /// <summary>Opens a non-writable, non-expandable empty stream.</summary>
    /// <returns>A readable empty stream.</returns>
    public Stream OpenReadStream() => new MemoryStream([], false);

    /// <summary>Tries to get the empty transport-text representation.</summary>
    /// <param name="text">An empty string.</param>
    /// <returns>Always <see langword="true" />.</returns>
    public bool TryGetTransportText([NotNullWhen(true)] out string? text)
    {
        text = string.Empty;
        return true;
    }
}
