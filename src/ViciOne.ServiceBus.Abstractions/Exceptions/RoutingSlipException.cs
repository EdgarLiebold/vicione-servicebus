using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to routing slip.</summary>
public class RoutingSlipException :
    CourierException
{
    /// <summary>Initializes a new instance.</summary>
    public RoutingSlipException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    public RoutingSlipException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="innerException">The inner exception.</param>
    public RoutingSlipException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
