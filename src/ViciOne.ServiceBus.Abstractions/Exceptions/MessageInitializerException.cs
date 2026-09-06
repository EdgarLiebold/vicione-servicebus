using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to message initializer.
/// </summary>
public class MessageInitializerException :
    ViciOneServiceBusException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public MessageInitializerException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="messageType">The message type value.</param>
    /// <param name="propertyName">The property name value.</param>
    /// <param name="propertType">The propert type value.</param>
    /// <param name="message">The message value.</param>
    public MessageInitializerException(string messageType, string propertyName, string propertType, string message)
        : base($"The {messageType} message initializer for property {propertyName}({propertType}) failed: {message}")
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public MessageInitializerException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
