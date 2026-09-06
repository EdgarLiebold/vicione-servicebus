using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to message data.</summary>
public class MessageDataException :
    ViciOneServiceBusException
{
    /// <summary>Initializes a new instance.</summary>
    public MessageDataException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    public MessageDataException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="innerException">The inner exception.</param>
    public MessageDataException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
