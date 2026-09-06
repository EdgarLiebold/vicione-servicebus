using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to pipe factory.
/// </summary>
[Serializable]
public class PipeFactoryException :
    ViciOneServiceBusException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public PipeFactoryException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public PipeFactoryException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public PipeFactoryException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
