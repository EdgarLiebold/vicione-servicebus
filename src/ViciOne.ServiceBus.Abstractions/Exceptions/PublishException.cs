using System;

namespace ViciOne.ServiceBus;

/// <summary>Reports that a message could not be published to its resolved destination.</summary>
public sealed class PublishException :
    ViciOneServiceBusException
{
    /// <summary>Creates a publish exception without a custom message.</summary>
    public PublishException()
    {
    }

    /// <summary>Creates a publish exception with the specified failure message.</summary>
    /// <param name="message">The description of the publish failure.</param>
    public PublishException(string message)
        : base(message)
    {
    }

    /// <summary>Creates a publish exception with an underlying failure.</summary>
    /// <param name="message">The description of the publish failure.</param>
    /// <param name="innerException">The exception that prevented the message from being published.</param>
    public PublishException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
