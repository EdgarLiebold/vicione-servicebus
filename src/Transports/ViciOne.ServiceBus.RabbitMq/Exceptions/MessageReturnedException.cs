using System;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Thrown when RabbitMQ returns a mandatory publish because it could not be routed.</summary>
public class MessageReturnedException :
    ViciOneServiceBusException
{
    /// <summary>Creates an exception without broker details.</summary>
    public MessageReturnedException()
    {
    }

    /// <summary>Creates an exception with a broker-return description.</summary>
    /// <param name="message">The failure description.</param>
    public MessageReturnedException(string message)
        : base(message)
    {
    }

    /// <summary>Creates an exception that retains the RabbitMQ client publish failure.</summary>
    /// <param name="message">The failure description.</param>
    /// <param name="innerException">The RabbitMQ client exception that reported the returned publish.</param>
    public MessageReturnedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
