using System;

namespace ViciOne.ServiceBus;

/// <summary>Provides the exception base for missing or invalid pipe-context payloads.</summary>
public class PayloadException :
    ViciOneServiceBusException
{
    /// <summary>Creates a payload exception without a custom message.</summary>
    public PayloadException()
    {
    }

    /// <summary>Creates a payload exception with the specified failure message.</summary>
    /// <param name="message">The description of the payload failure.</param>
    public PayloadException(string message)
        : base(message)
    {
    }

    /// <summary>Creates a payload exception with an underlying failure.</summary>
    /// <param name="message">The description of the payload failure.</param>
    /// <param name="innerException">The exception that caused payload access to fail.</param>
    public PayloadException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
