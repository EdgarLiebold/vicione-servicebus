using System;

namespace ViciOne.ServiceBus;

/// <summary>Reports that an operation requiring an active consume context was invoked outside message consumption.</summary>
public sealed class ConsumeContextNotAvailableException :
    ViciOneServiceBusException
{
    /// <summary>Creates an exception with the standard missing-context message.</summary>
    public ConsumeContextNotAvailableException()
        : this("A valid ConsumeContext was not available")
    {
    }

    /// <summary>Creates a missing-context exception with the specified failure message.</summary>
    /// <param name="message">The description of the unavailable context.</param>
    public ConsumeContextNotAvailableException(string message)
        : base(message)
    {
    }

    /// <summary>Creates a missing-context exception with an underlying failure.</summary>
    /// <param name="message">The description of the unavailable context.</param>
    /// <param name="innerException">The exception that prevented access to the consume context.</param>
    public ConsumeContextNotAvailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
