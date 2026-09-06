using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to payload factory.</summary>
public class PayloadFactoryException :
    PayloadException
{
    /// <summary>Initializes a new instance.</summary>
    public PayloadFactoryException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    public PayloadFactoryException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="innerException">The inner exception.</param>
    public PayloadFactoryException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
