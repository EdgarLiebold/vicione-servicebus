using System;
using System.IO;
using System.Linq;
using System.Text;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Exposes a selected byte-array segment as a UTF-8 message body.</summary>
public class ArrayMessageBody :
    MessageBody
{
    readonly ArraySegment<byte> _bytes;

    /// <summary>Creates a body over the selected array segment.</summary>
    /// <param name="bytes">The segment containing the body bytes; the default segment represents an empty body.</param>
    public ArrayMessageBody(ArraySegment<byte> bytes)
    {
        _bytes = bytes;
    }

    /// <summary>Gets the selected segment length in bytes.</summary>
    public long? Length => _bytes.Count;

    /// <summary>Opens a non-writable stream over the selected segment.</summary>
    /// <returns>A readable stream positioned at the start of the body.</returns>
    public Stream GetStream()
    {
        return _bytes.Array is { } array
            ? new MemoryStream(array, _bytes.Offset, _bytes.Count, false)
            : new MemoryStream([], false);
    }

    /// <summary>Copies the selected segment into a new byte array.</summary>
    /// <returns>The selected body bytes.</returns>
    public byte[] GetBytes()
    {
        return _bytes.Array is null ? [] : _bytes.ToArray();
    }

    /// <summary>Decodes the selected segment as UTF-8 text.</summary>
    /// <returns>The decoded body text.</returns>
    public string GetString()
    {
        return _bytes.Array is { } array
            ? Encoding.UTF8.GetString(array, _bytes.Offset, _bytes.Count)
            : string.Empty;
    }
}
