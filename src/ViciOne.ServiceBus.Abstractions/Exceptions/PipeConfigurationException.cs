using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to pipe configuration.</summary>
public class PipeConfigurationException :
    ViciOneServiceBusException
{
    /// <summary>Initializes a new instance.</summary>
    public PipeConfigurationException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    public PipeConfigurationException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="innerException">The inner exception.</param>
    public PipeConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
