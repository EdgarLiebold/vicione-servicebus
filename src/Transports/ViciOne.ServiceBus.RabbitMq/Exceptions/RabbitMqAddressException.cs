using System;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Represents an error related to rabbit mq address.
/// </summary>
public sealed class RabbitMqAddressException :
    ConfigurationException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public RabbitMqAddressException()
        : base("The RabbitMQ address is invalid.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public RabbitMqAddressException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public RabbitMqAddressException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
