using System;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Represents an error related to amazon sqs transport configuration.
/// </summary>
[Serializable]
public class AmazonSqsTransportConfigurationException :
    AmazonSqsTransportException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public AmazonSqsTransportConfigurationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public AmazonSqsTransportConfigurationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public AmazonSqsTransportConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
