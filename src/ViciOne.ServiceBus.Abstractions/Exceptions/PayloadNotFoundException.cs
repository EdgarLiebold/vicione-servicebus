using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to payload not found.</summary>
public class PayloadNotFoundException :
    PayloadException
{
    /// <summary>Initializes a new instance.</summary>
    public PayloadNotFoundException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    public PayloadNotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="innerException">The inner exception.</param>
    public PayloadNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
