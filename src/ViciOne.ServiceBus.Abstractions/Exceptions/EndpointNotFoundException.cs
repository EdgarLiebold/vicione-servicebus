using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to endpoint not found.</summary>
public class EndpointNotFoundException :
    ViciOneServiceBusException
{
    /// <summary>Initializes a new instance.</summary>
    public EndpointNotFoundException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    public EndpointNotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="innerException">The inner exception.</param>
    public EndpointNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
