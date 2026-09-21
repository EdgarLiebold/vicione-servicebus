using System;

namespace ViciOne.ServiceBus;

/// <summary>Reports a failure while executing a routing-slip activity.</summary>
public class ActivityExecutionException :
    CourierException
{
    /// <summary>Creates an activity-execution exception without a custom message.</summary>
    public ActivityExecutionException()
    {
    }

    /// <summary>Creates an activity-execution exception with the specified failure message.</summary>
    /// <param name="message">The description of the execution failure.</param>
    public ActivityExecutionException(string message)
        : base(message)
    {
    }

    /// <summary>Creates an activity-execution exception with an underlying failure.</summary>
    /// <param name="message">The description of the execution failure.</param>
    /// <param name="innerException">The exception that caused activity execution to fail.</param>
    public ActivityExecutionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
