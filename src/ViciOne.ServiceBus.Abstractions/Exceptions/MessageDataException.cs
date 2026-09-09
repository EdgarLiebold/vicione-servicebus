using System;

namespace ViciOne.ServiceBus;

/// <summary>Provides the exception base for external message-data storage and retrieval failures.</summary>
public class MessageDataException :
    ViciOneServiceBusException
{
    /// <summary>Creates a message-data exception without a custom message.</summary>
    public MessageDataException()
    {
    }

    /// <summary>Creates a message-data exception with the specified failure message.</summary>
    /// <param name="message">The description of the message-data failure.</param>
    public MessageDataException(string message)
        : base(message)
    {
    }

    /// <summary>Creates a message-data exception with an underlying failure.</summary>
    /// <param name="message">The description of the message-data failure.</param>
    /// <param name="innerException">The exception raised by the message-data repository.</param>
    public MessageDataException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
