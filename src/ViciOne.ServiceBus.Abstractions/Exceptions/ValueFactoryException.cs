using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to value factory.</summary>
public class ValueFactoryException :
    Exception
{
    /// <summary>Initializes a new instance.</summary>
    public ValueFactoryException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    public ValueFactoryException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="innerException">The inner exception.</param>
    public ValueFactoryException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
