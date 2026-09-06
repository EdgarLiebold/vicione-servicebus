using System;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Reports an invalid RabbitMQ host or endpoint address.</summary>
public sealed class RabbitMqAddressException :
    ConfigurationException
{
    /// <summary>Creates an exception with the default invalid-address message.</summary>
    public RabbitMqAddressException()
        : base("The RabbitMQ address is invalid.")
    {
    }

    /// <summary>Creates an exception with a specific address-validation message.</summary>
    /// <param name="message">The validation failure description.</param>
    public RabbitMqAddressException(string message)
        : base(message)
    {
    }

    /// <summary>Creates an exception that retains the underlying parsing failure.</summary>
    /// <param name="message">The validation failure description.</param>
    /// <param name="innerException">The exception raised while parsing or validating the address.</param>
    public RabbitMqAddressException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
