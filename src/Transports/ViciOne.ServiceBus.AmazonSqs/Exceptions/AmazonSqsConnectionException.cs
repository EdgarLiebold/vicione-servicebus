using System;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Represents an error related to amazon sqs connection.
/// </summary>
public class AmazonSqsConnectionException :
    ConnectionException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public AmazonSqsConnectionException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public AmazonSqsConnectionException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public AmazonSqsConnectionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
