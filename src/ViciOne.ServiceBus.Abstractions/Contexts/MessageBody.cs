using System.IO;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Defines the contract for message body.
/// </summary>
public interface MessageBody
{
    /// <summary>
    /// Gets the length value.
    /// </summary>
    long? Length { get; }

    /// <summary>
    /// Return the message body as a stream
    /// </summary>
    /// <returns></returns>
    Stream GetStream();

    /// <summary>
    /// Return the message body as a byte array
    /// </summary>
    /// <returns></returns>
    byte[] GetBytes();

    /// <summary>
    /// Return the message body as a string
    /// </summary>
    /// <returns></returns>
    string GetString();
}
