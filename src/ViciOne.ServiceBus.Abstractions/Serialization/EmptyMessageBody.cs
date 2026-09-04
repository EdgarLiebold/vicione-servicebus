using System.IO;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>
/// Provides an empty message body implementation.
/// </summary>
public class EmptyMessageBody :
    MessageBody
{
    /// <summary>
    /// Gets the instance value.
    /// </summary>
    public static MessageBody Instance { get; } = new EmptyMessageBody();

    /// <summary>
    /// Gets the length value.
    /// </summary>
    public long? Length => 0;

    /// <summary>
    /// Read-only and not expandable. The default constructor hands out a growable buffer, so a
    /// caller could have written a body into what is by definition empty.
    /// </summary>
    public Stream GetStream()
    {
        return new MemoryStream([], false);
    }

    /// <summary>
    /// Gets bytes.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public byte[] GetBytes()
    {
        return [];
    }

    /// <summary>
    /// Gets string.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public string GetString()
    {
        return string.Empty;
    }
}
