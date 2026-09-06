using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to message.</summary>
public class MessageException :
    ViciOneServiceBusException
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="innerException">The inner exception.</param>
    public MessageException(Type messageType, string message, Exception innerException)
        : base(message, innerException)
    {
        MessageType = messageType;
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="message">The message to process.</param>
    public MessageException(Type messageType, string message)
        : base(message)
    {
        MessageType = messageType;
    }

    /// <summary>Initializes a new instance.</summary>
    public MessageException()
    {
    }

    /// <summary>Gets or sets the message type.</summary>
    public Type? MessageType { get; private set; }
}
