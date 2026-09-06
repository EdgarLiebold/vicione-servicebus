using System;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Represents a failure while creating or using an ActiveMQ connection.</summary>
public class ActiveMqConnectionException :
    ConnectionException
{
    /// <summary>Creates an ActiveMQ connection exception.</summary>
    public ActiveMqConnectionException()
    {
    }

    /// <summary>Creates an ActiveMQ connection exception with an error message.</summary>
    /// <param name="message">The error message.</param>
    public ActiveMqConnectionException(string message)
        : base(message)
    {
    }

    /// <summary>Creates an ActiveMQ connection exception with an underlying failure.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The underlying connection failure.</param>
    public ActiveMqConnectionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
