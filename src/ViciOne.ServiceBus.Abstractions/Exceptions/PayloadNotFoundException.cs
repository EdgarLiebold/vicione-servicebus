using System;

namespace ViciOne.ServiceBus;

/// <summary>Reports that a required payload is absent from a pipe context.</summary>
public sealed class PayloadNotFoundException :
    PayloadException
{
    /// <summary>Creates a missing-payload exception without a custom message.</summary>
    public PayloadNotFoundException()
    {
    }

    /// <summary>Creates a missing-payload exception with the specified failure message.</summary>
    /// <param name="message">The description of the missing payload.</param>
    public PayloadNotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>Creates a missing-payload exception with an underlying failure.</summary>
    /// <param name="message">The description of the missing payload.</param>
    /// <param name="innerException">The exception that prevented payload retrieval.</param>
    public PayloadNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
