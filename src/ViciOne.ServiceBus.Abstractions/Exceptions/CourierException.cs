using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to courier.</summary>
public class CourierException :
    ViciOneServiceBusException,
    IRetryFailureClassification
{
    /// <summary>Initializes a new instance.</summary>
    public CourierException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    public CourierException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="innerException">The inner exception.</param>
    public CourierException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    RetryFailureKind IRetryFailureClassification.RetryFailureKind => RetryFailureKind.NonRetryable;
}
