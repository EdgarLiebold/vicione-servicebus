using System;

namespace ViciOne.ServiceBus;

/// <summary>Provides the exception base for failures associated with message consumers.</summary>
public class ConsumerException :
    ViciOneServiceBusException
{
    /// <summary>Creates a consumer exception without a custom message.</summary>
    public ConsumerException()
    {
    }

    /// <summary>Creates a consumer exception with the specified failure message.</summary>
    /// <param name="message">The description of the consumer failure.</param>
    public ConsumerException(string message)
        : base(message)
    {
    }

    /// <summary>Creates a consumer exception with an underlying failure.</summary>
    /// <param name="message">The description of the consumer failure.</param>
    /// <param name="innerException">The exception that caused the consumer to fail.</param>
    public ConsumerException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
