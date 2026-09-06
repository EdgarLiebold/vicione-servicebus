using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to consumer.</summary>
public class ConsumerException :
    ViciOneServiceBusException
{
    /// <summary>Initializes a new instance.</summary>
    public ConsumerException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    public ConsumerException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="innerException">The inner exception.</param>
    public ConsumerException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
