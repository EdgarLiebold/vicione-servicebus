using System.IO;
using System.Text;

namespace ViciOne.ServiceBus;

public class StringMessageBody :
    MessageBody
{
    readonly string _body;
    byte[]? _bytes;

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
    public Stream GetStream()
    {
        return new MemoryStream(GetBytes(), false);
    }

    /// <summary>
    /// A body made only of whitespace is a body. Discarding it here returned an empty array while
    /// <see cref="GetString" /> still returned the whitespace, so the two accessors disagreed about
    /// the same body and the reported length belonged to neither.
    /// </summary>
    public byte[] GetBytes()
    {
        return _bytes ??= _body != null
            ? Encoding.UTF8.GetBytes(_body)
            : [];
    }

    public string GetString()
    {
        return _body;
    }
}
