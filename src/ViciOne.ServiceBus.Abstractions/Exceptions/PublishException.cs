using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to publish.</summary>
public class PublishException :
    ViciOneServiceBusException
{
    /// <summary>Initializes a new instance.</summary>
    public PublishException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    public PublishException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="innerException">The inner exception.</param>
    public PublishException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
