using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to message initializer.</summary>
public class MessageInitializerException :
    ViciOneServiceBusException
{
    /// <summary>Initializes a new instance.</summary>
    public MessageInitializerException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="propertyName">The property name.</param>
    /// <param name="propertType">The runtime propert type used by the operation.</param>
    /// <param name="message">The message to process.</param>
    public MessageInitializerException(string messageType, string propertyName, string propertType, string message)
        : base($"The {messageType} message initializer for property {propertyName}({propertType}) failed: {message}")
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="innerException">The inner exception.</param>
    public MessageInitializerException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
