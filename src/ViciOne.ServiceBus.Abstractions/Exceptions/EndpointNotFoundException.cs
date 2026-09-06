using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to endpoint not found.
/// </summary>
public class EndpointNotFoundException :
    ViciOneServiceBusException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public EndpointNotFoundException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public EndpointNotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public EndpointNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
