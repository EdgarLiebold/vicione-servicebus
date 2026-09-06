using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to convention.</summary>
public class ConventionException :
    ViciOneServiceBusException
{
    /// <summary>Initializes a new instance.</summary>
    public ConventionException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    public ConventionException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="innerException">The inner exception.</param>
    public ConventionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
