using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to consume context not available.</summary>
public class ConsumeContextNotAvailableException :
    ViciOneServiceBusException
{
    /// <summary>Initializes a new instance.</summary>
    public ConsumeContextNotAvailableException()
        : this("A valid ConsumeContext was not available")
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    public ConsumeContextNotAvailableException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="innerException">The inner exception.</param>
    public ConsumeContextNotAvailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
