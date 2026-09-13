using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Messages;

/// <summary>Materializes the terminal event emitted when a routing slip completes successfully.</summary>
internal sealed class RoutingSlipCompletedMessage :
    IRoutingSlipCompleted
{
    /// <summary>Creates an empty instance for contract materialization.</summary>
    public RoutingSlipCompletedMessage()
    {
    }

    /// <summary>Creates a routing-slip-completed event with a detached final variable snapshot.</summary>
    /// <param name="trackingNumber">The routing-slip tracking number.</param>
    /// <param name="timestamp">The time when the routing slip completed.</param>
    /// <param name="duration">The elapsed time from routing-slip creation through completion.</param>
    /// <param name="variables">The final routing-slip variables.</param>
    public RoutingSlipCompletedMessage(Guid trackingNumber, DateTimeOffset timestamp, TimeSpan duration, IReadOnlyDictionary<string, object> variables)
    {
        RoutingSlipMessageState.Validate(trackingNumber, duration);

        Duration = duration;
        Timestamp = timestamp;

        TrackingNumber = trackingNumber;
        Variables = RoutingSlipMessageState.Snapshot(variables);
    }

    /// <summary>Gets or sets the routing-slip tracking number.</summary>
    public Guid TrackingNumber { get; set; }
    /// <summary>Gets or sets the time when the routing slip completed.</summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>Gets or sets the elapsed time from routing-slip creation through completion.</summary>
    public TimeSpan Duration { get; set; }
    /// <summary>Gets or sets the final routing-slip variables.</summary>
    public IReadOnlyDictionary<string, object> Variables { get; set; } = null!;
}
