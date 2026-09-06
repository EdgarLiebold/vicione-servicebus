using System;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Represents an error related to active mq connection.
/// </summary>
public class ActiveMqConnectionException :
    ConnectionException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public ActiveMqConnectionException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public ActiveMqConnectionException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public ActiveMqConnectionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
