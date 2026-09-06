using System;
using System.IO;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Carries memory message content.</summary>
public class MemoryMessageBody :
    MessageBody
{
    readonly ReadOnlyMemory<byte> _memory;
    byte[] _bytes = null!;
    string _string = null!;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="memory">The memory.</param>
    public MemoryMessageBody(ReadOnlyMemory<byte> memory)
    {
        _memory = memory;
    }

    /// <summary>Gets the length.</summary>
    public long? Length => _memory.Length;

    /// <summary>Gets stream.</summary>
    /// <returns>The stream.</returns>
    public Stream GetStream()
    {
        return new MemoryStream(GetBytes(), false);
    }

    /// <summary>Gets bytes.</summary>
    /// <returns>The bytes.</returns>
    public byte[] GetBytes()
    {
        return _bytes ??= _memory.ToArray();
    }

    /// <summary>Gets string.</summary>
    /// <returns>The string.</returns>
    public string GetString()
    {
        return _string ??= MessageDefaults.Encoding.GetString(_memory.Span);
    }
}
