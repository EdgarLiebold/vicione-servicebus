using System;

namespace ViciOne.ServiceBus;

/// <summary>Indicates that a routing-slip activity argument cannot be resolved or converted.</summary>
public sealed class RoutingSlipArgumentException :
    RoutingSlipException
{
    /// <summary>Creates an argument failure without additional diagnostic text.</summary>
    public RoutingSlipArgumentException()
    {
    }

    /// <summary>Creates an argument failure with diagnostic text.</summary>
    /// <param name="message">The error message that explains the argument failure.</param>
    public RoutingSlipArgumentException(string message)
        : base(message)
    {
    }

    /// <summary>Creates an argument failure with diagnostic text and its underlying cause.</summary>
    /// <param name="message">The error message that explains the argument failure.</param>
    /// <param name="innerException">The exception that caused the argument failure.</param>
    public RoutingSlipArgumentException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
