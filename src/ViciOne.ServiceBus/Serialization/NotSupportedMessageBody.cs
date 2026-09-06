using System;
using System.IO;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Carries not supported message content.</summary>
public class NotSupportedMessageBody :
    MessageBody
{
    /// <summary>Gets the length.</summary>
    public long? Length => throw new NotSupportedException();

    /// <summary>Gets stream.</summary>
    /// <returns>The stream.</returns>
    public Stream GetStream()
    {
        throw new NotSupportedException();
    }

    /// <summary>Gets bytes.</summary>
    /// <returns>The bytes.</returns>
    public byte[] GetBytes()
    {
        throw new NotSupportedException();
    }

    /// <summary>Gets string.</summary>
    /// <returns>The string.</returns>
    public string GetString()
    {
        throw new NotSupportedException();
    }
}
