using System;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Represents an error related to amazon sqs connect.
/// </summary>
public class AmazonSqsConnectException :
    AmazonSqsConnectionException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public AmazonSqsConnectException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public AmazonSqsConnectException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public AmazonSqsConnectException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
