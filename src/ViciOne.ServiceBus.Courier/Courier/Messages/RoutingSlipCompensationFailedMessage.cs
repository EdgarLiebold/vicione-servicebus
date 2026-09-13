using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Messages;

/// <summary>Materializes the terminal event emitted when routing-slip compensation cannot continue.</summary>
internal sealed class RoutingSlipCompensationFailedMessage :
    IRoutingSlipCompensationFailed
{
    /// <summary>Creates an empty instance for contract materialization.</summary>
    public RoutingSlipCompensationFailedMessage()
    {
    }

    /// <summary>Creates a compensation-failed event with a detached variable snapshot.</summary>
    /// <param name="host">The host on which compensation terminated.</param>
    /// <param name="trackingNumber">The routing-slip tracking number.</param>
    /// <param name="timestamp">The compensation-failure timestamp.</param>
    /// <param name="duration">The routing-slip duration before compensation failed.</param>
    /// <param name="exceptionInfo">The failure that terminated compensation.</param>
    /// <param name="variables">The routing-slip variables at failure.</param>
    public RoutingSlipCompensationFailedMessage(HostInfo host, Guid trackingNumber, DateTimeOffset timestamp, TimeSpan duration,
        ExceptionInfo exceptionInfo,
        IReadOnlyDictionary<string, object> variables)
    {
        ArgumentNullException.ThrowIfNull(host);
        RoutingSlipMessageState.Validate(trackingNumber, duration);
        ArgumentNullException.ThrowIfNull(exceptionInfo);

        Timestamp = timestamp;
        Duration = duration;
        Host = host;

        TrackingNumber = trackingNumber;
        Variables = RoutingSlipMessageState.Snapshot(variables);
        ExceptionInfo = exceptionInfo;
    }

    /// <summary>Gets or sets the routing-slip tracking number.</summary>
    public Guid TrackingNumber { get; set; }
    /// <summary>Gets or sets the failure that terminated compensation.</summary>
    public ExceptionInfo ExceptionInfo { get; set; } = null!;
    /// <summary>Gets or sets the routing-slip variables at failure.</summary>
    public IReadOnlyDictionary<string, object> Variables { get; set; } = null!;
    /// <summary>Gets or sets the host on which compensation terminated.</summary>
    public HostInfo Host { get; set; } = null!;
    /// <summary>Gets or sets the time when compensation terminated.</summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>Gets or sets the elapsed time from routing-slip creation until compensation terminated.</summary>
    public TimeSpan Duration { get; set; }
}
