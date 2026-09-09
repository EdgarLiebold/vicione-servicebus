using System;

namespace ViciOne.ServiceBus;

/// <summary>Reports a failure tied to a specific message contract type.</summary>
public class MessageException :
    ViciOneServiceBusException
{
    /// <summary>Creates a message exception with an underlying failure.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="message">The description of the message failure.</param>
    /// <param name="innerException">The exception that caused message processing to fail.</param>
    public MessageException(Type messageType, string message, Exception innerException)
        : base(message, innerException)
    {
        MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType));
    }

    /// <summary>Creates an exception for the specified message contract type.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="message">The description of the message failure.</param>
    public MessageException(Type messageType, string message)
        : base(message)
    {
        MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType));
    }

    /// <summary>Creates a message exception without contract-type context or a custom message.</summary>
    public MessageException()
    {
    }

    /// <summary>Gets the message contract type associated with the failure, when one was supplied.</summary>
    public Type? MessageType { get; }
}
