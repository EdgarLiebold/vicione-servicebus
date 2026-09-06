using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to shut down.</summary>
public class ShutDownException :
    ViciOneServiceBusException
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="innerException">The inner exception.</param>
    public ShutDownException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
