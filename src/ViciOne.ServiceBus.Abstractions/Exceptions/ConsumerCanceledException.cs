using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to consumer canceled.
/// </summary>
public class ConsumerCanceledException :
    ViciOneServiceBusException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public ConsumerCanceledException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public ConsumerCanceledException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public ConsumerCanceledException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
