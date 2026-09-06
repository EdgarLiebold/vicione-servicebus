using System.IO;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Carries empty message content.</summary>
public class EmptyMessageBody :
    MessageBody
{
    /// <summary>Gets the instance.</summary>
    public static MessageBody Instance { get; } = new EmptyMessageBody();

    /// <summary>Gets the length.</summary>
    public long? Length => 0;

    /// <summary>
    /// Read-only and not expandable. The default constructor hands out a growable buffer, so a
    /// caller could have written a body into what is by definition empty.
    /// </summary>
    /// <returns>The stream.</returns>
    public Stream GetStream()
    {
        return new MemoryStream([], false);
    }

    /// <summary>Gets bytes.</summary>
    /// <returns>The bytes.</returns>
    public byte[] GetBytes()
    {
        return [];
    }

    /// <summary>Gets string.</summary>
    /// <returns>The string.</returns>
    public string GetString()
    {
        return string.Empty;
    }
}
