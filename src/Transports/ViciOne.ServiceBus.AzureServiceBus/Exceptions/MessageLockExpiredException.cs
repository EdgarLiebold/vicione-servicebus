using System;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Represents an error related to message lock expired.
/// </summary>
[Serializable]
public class MessageLockExpiredException :
    TransportException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public MessageLockExpiredException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="uri">The uri value.</param>
    public MessageLockExpiredException(Uri uri)
        : base(uri)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="uri">The uri value.</param>
    /// <param name="message">The message value.</param>
    public MessageLockExpiredException(Uri uri, string message)
        : base(uri, message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="uri">The uri value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public MessageLockExpiredException(Uri uri, string message, Exception innerException)
        : base(uri, message, innerException)
    {
    }
}
