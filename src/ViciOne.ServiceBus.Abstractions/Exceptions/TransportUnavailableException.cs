using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to transport unavailable.
/// </summary>
public class TransportUnavailableException :
    ViciOneServiceBusException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public TransportUnavailableException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public TransportUnavailableException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public TransportUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
