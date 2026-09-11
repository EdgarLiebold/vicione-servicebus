using System.IO;
using System.Text;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Exposes a byte array as a UTF-8 message body, treating a missing array as empty.</summary>
public class BytesMessageBody :
    MessageBody
{
    readonly byte[] _bytes;
    string? _string;

    /// <summary>Creates a body over the supplied byte array.</summary>
    /// <param name="bytes">The body bytes, or <see langword="null" /> for an empty body.</param>
    public BytesMessageBody(byte[]? bytes)
    {
        _bytes = bytes ?? [];
    }

    /// <summary>Gets the current byte-array length.</summary>
    public long? Length => _bytes.Length;

    /// <summary>Opens a non-writable stream over the retained byte array.</summary>
    /// <returns>A readable stream positioned at the start of the body.</returns>
    public Stream GetStream()
    {
        return new MemoryStream(_bytes, false);
    }

    /// <summary>Gets the retained byte array.</summary>
    /// <returns>The same array used by this body.</returns>
    public byte[] GetBytes()
    {
        return _bytes;
    }

    /// <summary>Decodes the retained bytes as UTF-8 text.</summary>
    /// <returns>The decoded body text.</returns>
    public string GetString()
    {
        return _string ??= Encoding.UTF8.GetString(_bytes);
    }
}
