using System.IO;
using System.Text;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Carries bytes message content.</summary>
public class BytesMessageBody :
    MessageBody
{
    readonly byte[] _bytes;
    string? _string;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="bytes">The bytes.</param>
    public BytesMessageBody(byte[]? bytes)
    {
        _bytes = bytes ?? [];
    }

    /// <summary>Gets the length.</summary>
    public long? Length => _bytes.Length;

    /// <summary>
    /// Read-only, like every other message body: writing back through the stream a caller is handed
    /// cannot change what everybody else reads. That closes one route, not all of them — the
    /// constructor takes a caller's array and <see cref="GetBytes" /> hands it straight back, so
    /// this body is not immutable and is not claimed to be.
    /// </summary>
    /// <returns>The stream.</returns>
    public Stream GetStream()
    {
        return new MemoryStream(_bytes, false);
    }

    /// <summary>Gets bytes.</summary>
    /// <returns>The bytes.</returns>
    public byte[] GetBytes()
    {
        return _bytes;
    }

    /// <summary>Gets string.</summary>
    /// <returns>The string.</returns>
    public string GetString()
    {
        return _string ??= Encoding.UTF8.GetString(_bytes);
    }
}
