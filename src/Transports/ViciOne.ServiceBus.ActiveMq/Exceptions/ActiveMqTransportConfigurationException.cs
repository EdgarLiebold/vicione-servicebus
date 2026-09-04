using System;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Represents an error related to active mq transport configuration.
/// </summary>
[Serializable]
public class ActiveMqTransportConfigurationException :
    ActiveMqTransportException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public ActiveMqTransportConfigurationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public ActiveMqTransportConfigurationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public ActiveMqTransportConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
