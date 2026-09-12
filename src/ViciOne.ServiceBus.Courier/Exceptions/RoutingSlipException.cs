using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents a routing-slip construction, execution, or compensation failure.</summary>
public class RoutingSlipException :
    CourierException
{
    /// <summary>Creates a routing-slip failure without additional diagnostic text.</summary>
    public RoutingSlipException()
    {
    }

    /// <summary>Creates a routing-slip failure with diagnostic text.</summary>
    /// <param name="message">The error message that explains the failure.</param>
    public RoutingSlipException(string message)
        : base(message)
    {
    }

    /// <summary>Creates a routing-slip failure with diagnostic text and its underlying cause.</summary>
    /// <param name="message">The error message that explains the failure.</param>
    /// <param name="innerException">The exception that caused the routing-slip failure.</param>
    public RoutingSlipException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
