using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to courier.
/// </summary>
[Serializable]
public class CourierException :
    ViciOneServiceBusException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public CourierException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public CourierException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public CourierException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
