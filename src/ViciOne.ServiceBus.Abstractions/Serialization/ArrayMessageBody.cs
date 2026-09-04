using System;
using System.IO;
using System.Linq;
using System.Text;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>
/// Provides an array message body implementation.
/// </summary>
public class ArrayMessageBody :
    MessageBody
{
    readonly ArraySegment<byte> _bytes;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="bytes">The bytes value.</param>
    public ArrayMessageBody(ArraySegment<byte> bytes)
    {
        _bytes = bytes;
    }

    /// <summary>
    /// Gets the length value.
    /// </summary>
    public long? Length => _bytes.Count;

    /// <summary>
    /// Gets stream.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public Stream GetStream()
    {
        return new MemoryStream(_bytes.Array ?? throw new InvalidOperationException("Array not accessible"), _bytes.Offset, _bytes.Count, false);
    }

    /// <summary>
    /// Gets bytes.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public byte[] GetBytes()
    {
        return _bytes.ToArray();
    }

    /// <summary>
    /// Gets string.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public string GetString()
    {
        return Encoding.UTF8.GetString(_bytes.Array ?? throw new InvalidOperationException("Array not accessible"), _bytes.Offset, _bytes.Count);
    }
}
