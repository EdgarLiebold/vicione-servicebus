using System;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Represents an invalid ActiveMQ transport configuration.</summary>
public class ActiveMqTransportConfigurationException :
    ActiveMqTransportException
{
    /// <summary>Creates an ActiveMQ transport-configuration exception.</summary>
    public ActiveMqTransportConfigurationException()
    {
    }

    /// <summary>Creates an ActiveMQ transport-configuration exception with an error message.</summary>
    /// <param name="message">The configuration error message.</param>
    public ActiveMqTransportConfigurationException(string message)
        : base(message)
    {
    }

    /// <summary>Creates an ActiveMQ transport-configuration exception with an underlying failure.</summary>
    /// <param name="message">The configuration error message.</param>
    /// <param name="innerException">The underlying configuration failure.</param>
    public ActiveMqTransportConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
