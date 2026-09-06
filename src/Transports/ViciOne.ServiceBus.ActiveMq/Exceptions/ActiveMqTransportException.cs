using System;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Represents an error related to active mq transport.
/// </summary>
public class ActiveMqTransportException :
    ViciOneServiceBusException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public ActiveMqTransportException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public ActiveMqTransportException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public ActiveMqTransportException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
