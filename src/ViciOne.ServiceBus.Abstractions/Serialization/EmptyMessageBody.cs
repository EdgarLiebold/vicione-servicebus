using System.IO;

namespace ViciOne.ServiceBus;

public class EmptyMessageBody :
    MessageBody
{
    public static MessageBody Instance { get; } = new EmptyMessageBody();

    public long? Length => 0;

    /// <summary>
    /// Read-only and not expandable. The default constructor hands out a growable buffer, so a
    /// caller could have written a body into what is by definition empty.
    /// </summary>
    public Stream GetStream()
    {
        return new MemoryStream([], false);
    }

    public byte[] GetBytes()
    {
        return [];
    }

    public string GetString()
    {
        return string.Empty;
    }
}
