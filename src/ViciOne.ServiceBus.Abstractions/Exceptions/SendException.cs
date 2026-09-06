using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to send.</summary>
public class SendException :
    AbstractUriException
{
    /// <summary>Initializes a new instance.</summary>
    public SendException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="uri">The uri.</param>
    public SendException(Type messageType, Uri uri)
        : base(uri)
    {
        MessageType = messageType;
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="uri">The uri.</param>
    /// <param name="message">The message to process.</param>
    public SendException(Type messageType, Uri uri, string message)
        : base(uri, message)
    {
        MessageType = messageType;
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="uri">The uri.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="innerException">The inner exception.</param>
    public SendException(Type messageType, Uri uri, string message, Exception innerException)
        : base(uri, message, innerException)
    {
        MessageType = messageType;
    }

    /// <summary>Gets or sets the message type.</summary>
    public Type? MessageType { get; protected set; }
}
