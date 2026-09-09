using System;

namespace ViciOne.ServiceBus;

/// <summary>Provides the common base for failures reported by ViciOne ServiceBus.</summary>
public class ViciOneServiceBusException :
    Exception
{
    /// <summary>Creates a service-bus exception without a custom message.</summary>
    public ViciOneServiceBusException()
    {
    }

    /// <summary>Creates a service-bus exception with the specified failure message.</summary>
    /// <param name="message">The description of the failure.</param>
    public ViciOneServiceBusException(string? message)
        : base(message)
    {
    }

    /// <summary>Creates a service-bus exception with an underlying failure.</summary>
    /// <param name="message">The description of the failure.</param>
    /// <param name="innerException">The exception that caused this failure.</param>
    public ViciOneServiceBusException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
