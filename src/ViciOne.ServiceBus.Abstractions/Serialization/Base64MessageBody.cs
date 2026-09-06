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

    /// <summary>Initializes a new instance.</summary>
    /// <param name="text">The text.</param>
    public Base64MessageBody(string text)
    {
        _text = text;
    }

    /// <summary>Gets the decoded binary length in bytes rather than the length of the Base64 carrier text.</summary>
    public long? Length => GetBytes().LongLength;

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
        if (_bytes != null)
            return _bytes;

        _bytes = Convert.FromBase64String(_text);

        return _bytes;
    }

    /// <summary>Gets string.</summary>
    /// <returns>The string.</returns>
    public string GetString()
    {
        return _text;
    }
}
