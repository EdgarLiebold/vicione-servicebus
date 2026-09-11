using System;
using System.IO;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Exposes a read-only memory segment as a reusable message body.</summary>
public sealed class MemoryMessageBody :
    MessageBody
{
    readonly ReadOnlyMemory<byte> _memory;
    byte[]? _bytes;
    string? _string;

    /// <summary>Creates a body over the supplied memory segment.</summary>
    /// <param name="memory">The encoded message bytes.</param>
    public MemoryMessageBody(ReadOnlyMemory<byte> memory)
    {
        _memory = memory;
    }

    /// <summary>Gets the encoded byte length.</summary>
    public long? Length => _memory.Length;

    /// <summary>Opens a non-writable stream over a stable byte snapshot.</summary>
    /// <returns>A readable, non-writable stream positioned at the start of the body.</returns>
    public Stream GetStream()
    {
        return new MemoryStream(GetBytes(), false);
    }

    /// <summary>Gets a stable byte snapshot of the body.</summary>
    /// <returns>The encoded message bytes.</returns>
    public byte[] GetBytes()
    {
        return _bytes ??= _memory.ToArray();
    }

    /// <summary>Decodes the body with the ServiceBus message encoding.</summary>
    /// <returns>The decoded body text.</returns>
    public string GetString()
    {
        return _string ??= MessageDefaults.Encoding.GetString(_memory.Span);
    }
}
