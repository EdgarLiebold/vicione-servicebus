using System;
using System.IO;
using System.Text;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Exposes text and its UTF-8 encoding as a message body.</summary>
public class StringMessageBody :
    MessageBody
{
    readonly string _body;
    byte[]? _bytes;

    /// <summary>Creates a body from text.</summary>
    /// <param name="body">The complete body text.</param>
    public StringMessageBody(string body)
    {
        _body = body ?? throw new ArgumentNullException(nameof(body));
    }

    /// <summary>Gets the UTF-8 encoded body length in bytes.</summary>
    public long? Length => GetBytes().LongLength;

    /// <summary>Opens a non-writable stream over the cached UTF-8 bytes.</summary>
    /// <returns>A readable stream positioned at the start of the body.</returns>
    public Stream GetStream()
    {
        return new MemoryStream(GetBytes(), false);
    }

    /// <summary>Gets the cached UTF-8 representation without normalizing whitespace.</summary>
    /// <returns>The encoded body bytes retained by this instance.</returns>
    public byte[] GetBytes()
    {
        return _bytes ??= Encoding.UTF8.GetBytes(_body);
    }

    /// <summary>Gets the original body text.</summary>
    /// <returns>The text supplied to the constructor.</returns>
    public string GetString()
    {
        return _body;
    }
}
