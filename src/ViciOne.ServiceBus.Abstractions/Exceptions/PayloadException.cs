using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to payload.
/// </summary>
[Serializable]
public class PayloadException :
    ViciOneServiceBusException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public PayloadException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public PayloadException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public PayloadException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
