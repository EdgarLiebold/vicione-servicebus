using System.IO;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Defines the operations required by message body.</summary>
public interface MessageBody
{
    /// <summary>Gets the length.</summary>
    long? Length { get; }

    /// <summary>Return the message body as a stream.</summary>
    /// <returns>The stream.</returns>
    Stream GetStream();

    /// <summary>Return the message body as a byte array.</summary>
    /// <returns>The bytes.</returns>
    byte[] GetBytes();

    /// <summary>Return the message body as a string.</summary>
    /// <returns>The string.</returns>
    string GetString();
}
