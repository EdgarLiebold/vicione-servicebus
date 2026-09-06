using System;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Thrown when RabbitMQ does not acknowledge a publish that requires publisher confirmation.</summary>
public class MessageNotAcknowledgedException :
    TransportException
{
    /// <summary>Creates an exception without endpoint details.</summary>
    public MessageNotAcknowledgedException()
    {
    }

    /// <summary>Creates an exception for an unacknowledged publish.</summary>
    /// <param name="uri">The RabbitMQ destination address.</param>
    /// <param name="message">The failure description.</param>
    public MessageNotAcknowledgedException(Uri uri, string message)
        : base(uri, message)
    {
    }
}
