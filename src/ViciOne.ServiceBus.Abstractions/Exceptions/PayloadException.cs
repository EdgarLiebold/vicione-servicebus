using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to payload.</summary>
public class PayloadException :
    ViciOneServiceBusException
{
    /// <summary>Initializes a new instance.</summary>
    public PayloadException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    public PayloadException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="innerException">The inner exception.</param>
    public PayloadException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
