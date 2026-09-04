using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to send.
/// </summary>
[Serializable]
public class SendException :
    AbstractUriException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public SendException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="messageType">The message type value.</param>
    /// <param name="uri">The uri value.</param>
    public SendException(Type messageType, Uri uri)
        : base(uri)
    {
        MessageType = messageType;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="messageType">The message type value.</param>
    /// <param name="uri">The uri value.</param>
    /// <param name="message">The message value.</param>
    public SendException(Type messageType, Uri uri, string message)
        : base(uri, message)
    {
        MessageType = messageType;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="messageType">The message type value.</param>
    /// <param name="uri">The uri value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public SendException(Type messageType, Uri uri, string message, Exception innerException)
        : base(uri, message, innerException)
    {
        MessageType = messageType;
    }

    /// <summary>
    /// Gets or sets the message type value.
    /// </summary>
    public Type? MessageType { get; protected set; }
}
