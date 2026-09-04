using System;
using System.IO;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>
/// Provides a memory message body implementation.
/// </summary>
public class MemoryMessageBody :
    MessageBody
{
    readonly ReadOnlyMemory<byte> _memory;
    byte[] _bytes = null!;
    string _string = null!;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="memory">The memory value.</param>
    public MemoryMessageBody(ReadOnlyMemory<byte> memory)
    {
        _memory = memory;
    }

    /// <summary>
    /// Gets the length value.
    /// </summary>
    public long? Length => _memory.Length;

    /// <summary>
    /// Gets stream.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public Stream GetStream()
    {
        return new MemoryStream(GetBytes(), false);
    }

    /// <summary>
    /// Gets bytes.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public byte[] GetBytes()
    {
        return _bytes ??= _memory.ToArray();
    }

    /// <summary>
    /// Gets string.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public string GetString()
    {
        return _string ??= MessageDefaults.Encoding.GetString(_memory.Span);
    }
}
