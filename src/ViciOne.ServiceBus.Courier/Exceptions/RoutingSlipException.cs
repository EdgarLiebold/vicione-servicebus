using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents a routing-slip construction, execution, or compensation failure.</summary>
public class RoutingSlipException :
    CourierException
{
    /// <summary>Initializes a new instance.</summary>
    public RoutingSlipException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The error message that explains the failure.</param>
    public RoutingSlipException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The error message that explains the failure.</param>
    /// <param name="innerException">The exception that caused the routing-slip failure.</param>
    public RoutingSlipException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
