using System;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Represents an invalid Amazon SQS transport configuration.</summary>
public class AmazonSqsTransportConfigurationException :
    AmazonSqsTransportException
{
    /// <summary>Initializes an Amazon SQS configuration exception.</summary>
    public AmazonSqsTransportConfigurationException()
    {
    }

    /// <summary>Initializes an Amazon SQS configuration exception with an error message.</summary>
    /// <param name="message">The error message.</param>
    public AmazonSqsTransportConfigurationException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes an Amazon SQS configuration exception with an underlying failure.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The underlying failure.</param>
    public AmazonSqsTransportConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
