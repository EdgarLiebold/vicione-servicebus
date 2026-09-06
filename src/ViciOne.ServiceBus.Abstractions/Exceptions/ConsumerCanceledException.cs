using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to consumer canceled.</summary>
public class ConsumerCanceledException :
    ViciOneServiceBusException
{
    /// <summary>Initializes a new instance.</summary>
    public ConsumerCanceledException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    public ConsumerCanceledException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="innerException">The inner exception.</param>
    public ConsumerCanceledException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
