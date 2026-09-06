using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to message.
/// </summary>
public class MessageException :
    ViciOneServiceBusException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="messageType">The message type value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public MessageException(Type messageType, string message, Exception innerException)
        : base(message, innerException)
    {
        MessageType = messageType;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="messageType">The message type value.</param>
    /// <param name="message">The message value.</param>
    public MessageException(Type messageType, string message)
        : base(message)
    {
        MessageType = messageType;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public MessageException()
    {
    }

    /// <summary>
    /// Gets or sets the message type value.
    /// </summary>
    public Type? MessageType { get; private set; }
}
