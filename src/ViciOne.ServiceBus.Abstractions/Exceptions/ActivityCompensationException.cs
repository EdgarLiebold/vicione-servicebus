using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to activity compensation.</summary>
public class ActivityCompensationException :
    CourierException
{
    /// <summary>Initializes a new instance.</summary>
    public ActivityCompensationException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    public ActivityCompensationException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="innerException">The inner exception.</param>
    public ActivityCompensationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
