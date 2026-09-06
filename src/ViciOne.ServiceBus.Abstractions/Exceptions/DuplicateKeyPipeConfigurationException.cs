using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to duplicate key pipe configuration.</summary>
public class DuplicateKeyPipeConfigurationException :
    PipeConfigurationException
{
    /// <summary>Initializes a new instance.</summary>
    public DuplicateKeyPipeConfigurationException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    public DuplicateKeyPipeConfigurationException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="innerException">The inner exception.</param>
    public DuplicateKeyPipeConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
