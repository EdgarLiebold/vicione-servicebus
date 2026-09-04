using System;
using System.IO;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>
/// Provides a not supported message body implementation.
/// </summary>
public class NotSupportedMessageBody :
    MessageBody
{
    /// <summary>
    /// Gets the length value.
    /// </summary>
    public long? Length => throw new NotSupportedException();

    /// <summary>
    /// Gets stream.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public Stream GetStream()
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Gets bytes.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public byte[] GetBytes()
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Gets string.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public string GetString()
    {
        throw new NotSupportedException();
    }
}
