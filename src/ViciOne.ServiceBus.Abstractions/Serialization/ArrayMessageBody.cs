using System;
using System.IO;
using System.Linq;
using System.Text;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Carries array message content.</summary>
public class ArrayMessageBody :
    MessageBody
{
    readonly ArraySegment<byte> _bytes;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="bytes">The bytes.</param>
    public ArrayMessageBody(ArraySegment<byte> bytes)
    {
        _bytes = bytes;
    }

    /// <summary>Gets the length.</summary>
    public long? Length => _bytes.Count;

    /// <summary>Gets stream.</summary>
    /// <returns>The stream.</returns>
    public Stream GetStream()
    {
        return new MemoryStream(_bytes.Array ?? throw new InvalidOperationException("Array not accessible"), _bytes.Offset, _bytes.Count, false);
    }

    /// <summary>Gets bytes.</summary>
    /// <returns>The bytes.</returns>
    public byte[] GetBytes()
    {
        return _bytes.ToArray();
    }

    /// <summary>Gets string.</summary>
    /// <returns>The string.</returns>
    public string GetString()
    {
        return Encoding.UTF8.GetString(_bytes.Array ?? throw new InvalidOperationException("Array not accessible"), _bytes.Offset, _bytes.Count);
    }
}
