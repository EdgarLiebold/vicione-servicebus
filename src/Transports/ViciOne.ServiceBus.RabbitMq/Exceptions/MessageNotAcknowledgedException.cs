using System;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Thrown when a message is not acknowledged by the broker
/// </summary>
public class MessageNotAcknowledgedException :
    TransportException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public MessageNotAcknowledgedException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="uri">The uri value.</param>
    /// <param name="message">The message value.</param>
    public MessageNotAcknowledgedException(Uri uri, string message)
        : base(uri, message)
    {
    }
}
