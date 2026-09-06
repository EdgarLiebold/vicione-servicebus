using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to routing slip argument.
/// </summary>
public class RoutingSlipArgumentException :
    RoutingSlipException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public RoutingSlipArgumentException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public RoutingSlipArgumentException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public RoutingSlipArgumentException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
