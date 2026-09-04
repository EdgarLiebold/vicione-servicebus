using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Messages;

/// <summary>
/// Provides a routing slip compensation failed message implementation.
/// </summary>
[Serializable]
public class RoutingSlipCompensationFailedMessage :
    RoutingSlipCompensationFailed
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public RoutingSlipCompensationFailedMessage()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="host">The host value.</param>
    /// <param name="trackingNumber">The tracking number value.</param>
    /// <param name="failureTimestamp">The failure timestamp value.</param>
    /// <param name="routingSlipDuration">The routing slip duration value.</param>
    /// <param name="exceptionInfo">The exception info value.</param>
    /// <param name="variables">The variables value.</param>
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

    /// <summary>
    /// Gets or sets the tracking number value.
    /// </summary>
    public Guid TrackingNumber { get; set; }
    /// <summary>
    /// Gets or sets the exception info value.
    /// </summary>
    public ExceptionInfo ExceptionInfo { get; set; } = null!;
    /// <summary>
    /// Gets or sets the variables value.
    /// </summary>
    public IDictionary<string, object> Variables { get; set; } = null!;
    /// <summary>
    /// Gets or sets the host value.
    /// </summary>
    public HostInfo Host { get; set; } = null!;
    /// <summary>
    /// Gets or sets the timestamp value.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>
    /// Gets or sets the duration value.
    /// </summary>
    public TimeSpan Duration { get; set; }
}
