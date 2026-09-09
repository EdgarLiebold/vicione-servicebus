using System;

namespace ViciOne.ServiceBus;

/// <summary>Reports that a consumer cannot process a received message.</summary>
public sealed class ConsumerMessageException :
    ConsumerException
{
    /// <summary>Creates a consumer-message exception without a custom message.</summary>
    public ConsumerMessageException()
    {
    }

    /// <summary>Creates a consumer-message exception with the specified failure message.</summary>
    /// <param name="message">The description of the message-processing failure.</param>
    public ConsumerMessageException(string message)
        : base(message)
    {
    }

    /// <summary>Creates a consumer-message exception with an underlying failure.</summary>
    /// <param name="message">The description of the message-processing failure.</param>
    /// <param name="innerException">The exception raised while the consumer processed the message.</param>
    public ConsumerMessageException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
