using System;

namespace ViciOne.ServiceBus;

/// <summary>Reports that no consumer accepted a message delivered to an endpoint.</summary>
public class MessageNotConsumedException :
    TransportException
{
    /// <summary>Creates an unconsumed-message exception without endpoint context.</summary>
    public MessageNotConsumedException()
    {
    }

    /// <summary>Creates an unconsumed-message exception for the specified endpoint.</summary>
    /// <param name="uri">The endpoint that received the unconsumed message.</param>
    public MessageNotConsumedException(Uri uri)
        : base(uri)
    {
    }

    /// <summary>Creates an unconsumed-message exception for the specified endpoint.</summary>
    /// <param name="uri">The endpoint that received the unconsumed message.</param>
    /// <param name="message">The description of why the message was not consumed.</param>
    public MessageNotConsumedException(Uri uri, string message)
        : base(uri, message)
    {
    }

    /// <summary>Creates an unconsumed-message exception with an underlying failure.</summary>
    /// <param name="uri">The endpoint that received the unconsumed message.</param>
    /// <param name="message">The description of why the message was not consumed.</param>
    /// <param name="innerException">The exception that prevented the message from being consumed.</param>
    public MessageNotConsumedException(Uri uri, string message, Exception innerException)
        : base(uri, message, innerException)
    {
    }
}
