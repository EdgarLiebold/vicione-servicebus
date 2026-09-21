using System;

namespace ViciOne.ServiceBus;

/// <summary>Reports that a routing-slip activity explicitly returned a faulted execution result.</summary>
public sealed class ActivityExecutionFaultedException :
    ActivityExecutionException
{
    /// <summary>Creates an exception for an activity fault that did not provide a specific cause.</summary>
    public ActivityExecutionFaultedException()
        : this("The routing slip activity execution faulted with an unspecified exception")
    {
    }

    /// <summary>Creates an activity-fault exception with the specified failure message.</summary>
    /// <param name="message">The description supplied by the faulted activity.</param>
    public ActivityExecutionFaultedException(string message)
        : base(message)
    {
    }

    /// <summary>Creates an activity-fault exception with an underlying failure.</summary>
    /// <param name="message">The description supplied by the faulted activity.</param>
    /// <param name="innerException">The exception reported by the activity.</param>
    public ActivityExecutionFaultedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
