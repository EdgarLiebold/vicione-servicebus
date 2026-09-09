using System;

namespace ViciOne.ServiceBus;

/// <summary>Reports a failure while compensating an executed routing-slip activity.</summary>
public sealed class ActivityCompensationException :
    CourierException
{
    /// <summary>Creates an activity-compensation exception without a custom message.</summary>
    public ActivityCompensationException()
    {
    }

    /// <summary>Creates an activity-compensation exception with the specified failure message.</summary>
    /// <param name="message">The description of the compensation failure.</param>
    public ActivityCompensationException(string message)
        : base(message)
    {
    }

    /// <summary>Creates an activity-compensation exception with an underlying failure.</summary>
    /// <param name="message">The description of the compensation failure.</param>
    /// <param name="innerException">The exception that caused compensation to fail.</param>
    public ActivityCompensationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
