using System;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Represents an ActiveMQ transport failure.</summary>
public class ActiveMqTransportException :
    ViciOneServiceBusException
{
    /// <summary>Creates an ActiveMQ transport exception.</summary>
    public ActiveMqTransportException()
    {
    }

    /// <summary>Creates an ActiveMQ transport exception with an error message.</summary>
    /// <param name="message">The transport error message.</param>
    public ActiveMqTransportException(string message)
        : base(message)
    {
    }

    /// <summary>Creates an ActiveMQ transport exception with an underlying failure.</summary>
    /// <param name="message">The transport error message.</param>
    /// <param name="innerException">The underlying transport failure.</param>
    public ActiveMqTransportException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
