using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to consumer message.</summary>
public class ConsumerMessageException :
    ConsumerException
{
    /// <summary>Initializes a new instance.</summary>
    public ConsumerMessageException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    public ConsumerMessageException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="innerException">The inner exception.</param>
    public ConsumerMessageException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
