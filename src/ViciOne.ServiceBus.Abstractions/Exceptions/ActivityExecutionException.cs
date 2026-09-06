using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to activity execution.
/// </summary>
public class ActivityExecutionException :
    CourierException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public ActivityExecutionException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public ActivityExecutionException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public ActivityExecutionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
