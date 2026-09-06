using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Messages;

/// <summary>Carries routing slip compensation failed message data.</summary>
public class RoutingSlipCompensationFailedMessage :
    RoutingSlipCompensationFailed
{
    /// <summary>Initializes a new instance.</summary>
    public RoutingSlipCompensationFailedMessage()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="host">The host.</param>
    /// <param name="trackingNumber">The tracking number.</param>
    /// <param name="failureTimestamp">The failure timestamp.</param>
    /// <param name="routingSlipDuration">The routing slip duration.</param>
    /// <param name="exceptionInfo">The exception info.</param>
    /// <param name="variables">The variables.</param>
    public RoutingSlipCompensationFailedMessage(HostInfo host, Guid trackingNumber, DateTimeOffset failureTimestamp, TimeSpan routingSlipDuration,
        ExceptionInfo exceptionInfo,
        IDictionary<string, object> variables)
    {
        Timestamp = failureTimestamp;
        Duration = routingSlipDuration;
        Host = host;

        TrackingNumber = trackingNumber;
        Variables = variables;
        ExceptionInfo = exceptionInfo;
    }

    /// <summary>Gets or sets the tracking number.</summary>
    public Guid TrackingNumber { get; set; }
    /// <summary>Gets or sets the exception info.</summary>
    public ExceptionInfo ExceptionInfo { get; set; } = null!;
    /// <summary>Gets or sets the variables.</summary>
    public IDictionary<string, object> Variables { get; set; } = null!;
    /// <summary>Gets or sets the host.</summary>
    public HostInfo Host { get; set; } = null!;
    /// <summary>Gets or sets the timestamp.</summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>Gets or sets the duration.</summary>
    public TimeSpan Duration { get; set; }
}
