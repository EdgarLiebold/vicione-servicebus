using System;
using System.IO;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>
/// Carries a binary message body that arrived as Base64 text, so a transport that cannot carry bytes
/// can still deliver one.
/// <para>
/// <see cref="GetString" /> returns the text as it was given. <see cref="GetBytes" /> and
/// <see cref="GetStream" /> decode it; they are not pass-through, and the summary that said so
/// described neither this code nor the length it reports.
/// </para>
/// </summary>
public class Base64MessageBody :
    MessageBody
{
    readonly string _text;
    byte[]? _bytes;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="text">The text value.</param>
    public Base64MessageBody(string text)
    {
        _text = text;
    }

    /// <summary>
    /// The number of bytes this body transmits, which is the decoded binary, not the Base64 text
    /// that carries it. The text is roughly a third longer than the body it encodes, so reporting
    /// its character count overstated every body of this kind.
    /// </summary>
    public long? Length => GetBytes().LongLength;

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
        if (_bytes != null)
            return _bytes;

        _bytes = Convert.FromBase64String(_text);

        return _bytes;
    }

    /// <summary>
    /// Gets string.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public string GetString()
    {
        return _text;
    }
}
