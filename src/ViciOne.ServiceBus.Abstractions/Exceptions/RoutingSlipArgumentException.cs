using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to routing slip argument.</summary>
public class RoutingSlipArgumentException :
    RoutingSlipException
{
    /// <summary>Initializes a new instance.</summary>
    public RoutingSlipArgumentException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    public RoutingSlipArgumentException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="innerException">The inner exception.</param>
    public RoutingSlipArgumentException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
