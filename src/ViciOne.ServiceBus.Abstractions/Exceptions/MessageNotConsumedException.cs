using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to message not consumed.
/// </summary>
public class MessageNotConsumedException :
    TransportException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public MessageNotConsumedException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="uri">The uri value.</param>
    public MessageNotConsumedException(Uri uri)
        : base(uri)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="uri">The uri value.</param>
    /// <param name="message">The message value.</param>
    public MessageNotConsumedException(Uri uri, string message)
        : base(uri, message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="uri">The uri value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public MessageNotConsumedException(Uri uri, string message, Exception innerException)
        : base(uri, message, innerException)
    {
    }
}
