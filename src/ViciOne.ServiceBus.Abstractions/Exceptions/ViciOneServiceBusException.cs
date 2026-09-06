using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to vici one service bus.
/// </summary>
public class ViciOneServiceBusException :
    Exception
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public ViciOneServiceBusException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public ViciOneServiceBusException(string? message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public ViciOneServiceBusException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
