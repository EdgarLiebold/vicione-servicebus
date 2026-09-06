using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to activity execution faulted.</summary>
public class ActivityExecutionFaultedException :
    ActivityExecutionException
{
    /// <summary>Initializes a new instance.</summary>
    public ActivityExecutionFaultedException()
        : this("The routing slip activity execution faulted with an unspecified exception")
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    public ActivityExecutionFaultedException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="innerException">The inner exception.</param>
    public ActivityExecutionFaultedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
