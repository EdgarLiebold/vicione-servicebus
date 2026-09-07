using System;

namespace ViciOne.ServiceBus;

/// <summary>Indicates that a routing-slip activity argument cannot be resolved or converted.</summary>
public sealed class RoutingSlipArgumentException :
    RoutingSlipException
{
    /// <summary>Initializes a new instance.</summary>
    public RoutingSlipArgumentException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The error message that explains the argument failure.</param>
    public RoutingSlipArgumentException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The error message that explains the argument failure.</param>
    /// <param name="innerException">The exception that caused the argument failure.</param>
    public RoutingSlipArgumentException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
