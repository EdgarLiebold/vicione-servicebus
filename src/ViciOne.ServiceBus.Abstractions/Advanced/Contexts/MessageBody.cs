using System.IO;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides repeatable read access to a serialized message body.</summary>
public interface MessageBody
{
    /// <summary>Gets the body length in bytes when it is known.</summary>
    long? Length { get; }

    /// <summary>Gets a readable stream containing the body bytes.</summary>
    /// <returns>A readable body stream positioned at its beginning.</returns>
    Stream GetStream();

    /// <summary>Gets the body bytes.</summary>
    /// <returns>A byte array containing the complete body.</returns>
    byte[] GetBytes();

    /// <summary>Gets the body as text using the representation defined by the implementation.</summary>
    /// <returns>The complete body text.</returns>
    string GetString();
}
