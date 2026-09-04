using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to duplicate key pipe configuration.
/// </summary>
[Serializable]
public class DuplicateKeyPipeConfigurationException :
    PipeConfigurationException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public DuplicateKeyPipeConfigurationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public DuplicateKeyPipeConfigurationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public DuplicateKeyPipeConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
