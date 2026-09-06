using System;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Represents an error related to amazon sqs transport.
/// </summary>
public class AmazonSqsTransportException :
    ViciOneServiceBusException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public AmazonSqsTransportException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public AmazonSqsTransportException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public AmazonSqsTransportException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
