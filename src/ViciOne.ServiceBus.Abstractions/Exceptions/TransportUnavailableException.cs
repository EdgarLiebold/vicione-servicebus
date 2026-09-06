using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to transport unavailable.</summary>
public class TransportUnavailableException :
    ViciOneServiceBusException
{
    /// <summary>Initializes a new instance.</summary>
    public TransportUnavailableException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    public TransportUnavailableException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="innerException">The inner exception.</param>
    public TransportUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
