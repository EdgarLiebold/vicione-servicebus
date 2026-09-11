using System.IO;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Represents a message body with no content.</summary>
public class EmptyMessageBody :
    MessageBody
{
    /// <summary>Gets the shared empty message body.</summary>
    public static MessageBody Instance { get; } = new EmptyMessageBody();

    /// <summary>Gets the zero-byte body length.</summary>
    public long? Length => 0;

    /// <summary>Opens a non-writable, non-expandable empty stream.</summary>
    /// <returns>A readable empty stream.</returns>
    public Stream GetStream()
    {
        return new MemoryStream([], false);
    }

    /// <summary>Creates an empty byte array.</summary>
    /// <returns>An empty array.</returns>
    public byte[] GetBytes()
    {
        return [];
    }

    /// <summary>Gets the empty text representation.</summary>
    /// <returns>An empty string.</returns>
    public string GetString()
    {
        return string.Empty;
    }
}
