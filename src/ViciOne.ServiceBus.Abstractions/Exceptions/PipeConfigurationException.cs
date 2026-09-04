using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to pipe configuration.
/// </summary>
[Serializable]
public class PipeConfigurationException :
    ViciOneServiceBusException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public PipeConfigurationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public PipeConfigurationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public PipeConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
