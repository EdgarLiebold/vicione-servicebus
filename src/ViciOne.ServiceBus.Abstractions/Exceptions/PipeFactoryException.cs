using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to pipe factory.</summary>
public class PipeFactoryException :
    ViciOneServiceBusException
{
    /// <summary>Initializes a new instance.</summary>
    public PipeFactoryException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    public PipeFactoryException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="innerException">The inner exception.</param>
    public PipeFactoryException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
