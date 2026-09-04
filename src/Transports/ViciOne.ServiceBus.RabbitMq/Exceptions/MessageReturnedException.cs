using System;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Published when a RabbitMQ channel is closed and the message was not confirmed by the broker.
/// </summary>
[Serializable]
public class MessageReturnedException :
    ViciOneServiceBusException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public MessageReturnedException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public MessageReturnedException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public MessageReturnedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
