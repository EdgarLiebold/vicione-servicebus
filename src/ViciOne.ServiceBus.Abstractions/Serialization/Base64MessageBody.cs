using System;
using System.IO;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>
/// Carries a binary message body that arrived as Base64 text, so a transport that cannot carry bytes
/// can still deliver one.
/// <para>
/// <see cref="GetString" /> returns the text as it was given. <see cref="GetBytes" /> and
/// <see cref="GetStream" /> return the decoded binary content.
/// </para>
/// </summary>
public class Base64MessageBody :
    MessageBody
{
    readonly string _text;
    byte[]? _bytes;

    /// <summary>Creates a body from Base64 carrier text.</summary>
    /// <param name="text">The Base64 text retained as the text representation.</param>
    public Base64MessageBody(string text)
    {
        _text = text ?? throw new ArgumentNullException(nameof(text));
    }

    /// <summary>Gets the decoded binary length in bytes rather than the length of the Base64 carrier text.</summary>
    public long? Length => GetBytes().LongLength;

    /// <summary>Opens a non-writable stream over the decoded bytes.</summary>
    /// <returns>A readable stream positioned at the start of the decoded body.</returns>
    public Stream GetStream()
    {
        return new MemoryStream(GetBytes(), false);
    }

    /// <summary>Gets the decoded binary content.</summary>
    /// <returns>The decoded body bytes retained by this instance.</returns>
    public byte[] GetBytes()
    {
        if (_bytes != null)
            return _bytes;

        _bytes = Convert.FromBase64String(_text);

        return _bytes;
    }

    /// <summary>Gets the original Base64 carrier text.</summary>
    /// <returns>The text supplied to the constructor.</returns>
    public string GetString()
    {
        return _text;
    }
}
