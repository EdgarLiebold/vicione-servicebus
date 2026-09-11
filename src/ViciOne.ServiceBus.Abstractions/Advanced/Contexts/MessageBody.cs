using System.IO;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Describes a message body's serialized size and the representations available from its source.</summary>
public interface MessageBody
{
    /// <summary>Gets the body length in bytes when it is known.</summary>
    long? Length { get; }

    /// <summary>Opens the serialized body as a readable stream when the source materializes body content.</summary>
    /// <returns>A readable body stream positioned at its beginning; the caller owns the returned stream.</returns>
    /// <exception cref="NotSupportedException">The source measures the body but does not materialize serialized content.</exception>
    Stream GetStream();

    /// <summary>Gets the serialized body bytes when the source materializes body content.</summary>
    /// <returns>A byte array containing the complete body; ownership is defined by the implementation.</returns>
    /// <exception cref="NotSupportedException">The source measures the body but does not materialize serialized content.</exception>
    byte[] GetBytes();

    /// <summary>Gets the body text using the representation defined by the source.</summary>
    /// <returns>The complete body text.</returns>
    /// <exception cref="NotSupportedException">The source measures the body but does not materialize serialized content.</exception>
    string GetString();
}
