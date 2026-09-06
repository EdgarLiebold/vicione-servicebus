using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to vici one service bus.</summary>
public class ViciOneServiceBusException :
    Exception
{
    /// <summary>Initializes a new instance.</summary>
    public ViciOneServiceBusException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    public ViciOneServiceBusException(string? message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="innerException">The inner exception.</param>
    public ViciOneServiceBusException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
