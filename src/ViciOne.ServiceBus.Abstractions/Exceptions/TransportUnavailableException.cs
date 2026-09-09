using System;

namespace ViciOne.ServiceBus;

/// <summary>Reports a transient failure because a configured transport is unavailable.</summary>
public sealed class TransportUnavailableException :
    ViciOneServiceBusException
{
    /// <summary>Creates a transport-unavailable exception without a custom message.</summary>
    public TransportUnavailableException()
    {
    }

    /// <summary>Creates a transport-unavailable exception with the specified failure message.</summary>
    /// <param name="message">The description of the availability failure.</param>
    public TransportUnavailableException(string message)
        : base(message)
    {
    }

    /// <summary>Creates a transport-unavailable exception with an underlying failure.</summary>
    /// <param name="message">The description of the availability failure.</param>
    /// <param name="innerException">The exception raised while accessing the transport.</param>
    public TransportUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
