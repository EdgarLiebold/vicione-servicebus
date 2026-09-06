using System;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Represents an Amazon SQS or Amazon SNS transport operation failure.</summary>
public class AmazonSqsTransportException :
    ViciOneServiceBusException
{
    /// <summary>Initializes an Amazon transport exception.</summary>
    public AmazonSqsTransportException()
    {
    }

    /// <summary>Initializes an Amazon transport exception with an error message.</summary>
    /// <param name="message">The error message.</param>
    public AmazonSqsTransportException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes an Amazon transport exception with an underlying failure.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The underlying failure.</param>
    public AmazonSqsTransportException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
