using System.IO;
using System.Text;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Carries string message content.</summary>
public class StringMessageBody :
    MessageBody
{
    readonly string _body;
    byte[]? _bytes;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="body">The body.</param>
    public StringMessageBody(string body)
    {
        _body = body;
    }

    /// <summary>
    /// The number of bytes this body transmits, which is by definition the length of what
    /// <see cref="GetBytes" /> returns, whichever accessor ran first. Counting characters instead
    /// understated every body carrying a character outside ASCII.
    /// </summary>
    public long? Length => GetBytes().LongLength;

    /// <summary>
    /// Read-only: writing back through this stream cannot change what everybody else reads. The
    /// array from <see cref="GetBytes" /> is still a caller's to write into, so this is one closed
    /// route rather than immutability.
    /// </summary>
    /// <returns>The stream.</returns>
    public Stream GetStream()
    {
        return new MemoryStream(GetBytes(), false);
    }

    /// <summary>
    /// A body made only of whitespace is a body. Discarding it here returned an empty array while
    /// <see cref="GetString" /> still returned the whitespace, so the two accessors disagreed about
    /// the same body and the reported length belonged to neither.
    /// </summary>
    /// <returns>The bytes.</returns>
    public byte[] GetBytes()
    {
        return _bytes ??= _body != null
            ? Encoding.UTF8.GetBytes(_body)
            : [];
    }

    /// <summary>Gets string.</summary>
    /// <returns>The string.</returns>
    public string GetString()
    {
        return _body;
    }
}
