using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to activity execution.</summary>
public class ActivityExecutionException :
    CourierException
{
    /// <summary>Initializes a new instance.</summary>
    public ActivityExecutionException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    public ActivityExecutionException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="innerException">The inner exception.</param>
    public ActivityExecutionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
