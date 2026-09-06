using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to produce.</summary>
public class ProduceException :
    ViciOneServiceBusException
{
    /// <summary>Initializes a new instance.</summary>
    public ProduceException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    public ProduceException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="innerException">The inner exception.</param>
    public ProduceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
