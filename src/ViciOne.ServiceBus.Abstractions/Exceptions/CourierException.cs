using System;

namespace ViciOne.ServiceBus;

/// <summary>Provides the non-retryable exception base for routing-slip execution and compensation failures.</summary>
public class CourierException :
    ViciOneServiceBusException,
    IRetryFailureClassification
{
    /// <summary>Creates a routing-slip exception without a custom message.</summary>
    public CourierException()
    {
    }

    /// <summary>Creates a routing-slip exception with the specified failure message.</summary>
    /// <param name="message">The description of the routing-slip failure.</param>
    public CourierException(string message)
        : base(message)
    {
    }

    /// <summary>Creates a routing-slip exception with an underlying failure.</summary>
    /// <param name="message">The description of the routing-slip failure.</param>
    /// <param name="innerException">The exception that caused routing-slip processing to fail.</param>
    public CourierException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    RetryFailureKind IRetryFailureClassification.RetryFailureKind => RetryFailureKind.NonRetryable;
}
