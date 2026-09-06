using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to activity compensation.
/// </summary>
public class ActivityCompensationException :
    CourierException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public ActivityCompensationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public ActivityCompensationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public ActivityCompensationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
