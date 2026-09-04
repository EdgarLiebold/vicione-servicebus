using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to activity execution faulted.
/// </summary>
[Serializable]
public class ActivityExecutionFaultedException :
    ActivityExecutionException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public ActivityExecutionFaultedException()
        : this("The routing slip activity execution faulted with an unspecified exception")
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public ActivityExecutionFaultedException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public ActivityExecutionFaultedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
