using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Messages;

/// <summary>Materializes the terminal event emitted after a routing slip faults and compensation ends.</summary>
internal sealed class RoutingSlipFaultedMessage :
    RoutingSlipFaulted
{
    /// <summary>Creates an empty instance for contract materialization.</summary>
    public RoutingSlipFaultedMessage()
    {
    }

    /// <summary>Creates a routing-slip-faulted event with detached failure and variable snapshots.</summary>
    /// <param name="trackingNumber">The routing-slip tracking number.</param>
    /// <param name="timestamp">The time when the routing slip reached its terminal fault.</param>
    /// <param name="duration">The elapsed time from routing-slip creation through the terminal fault.</param>
    /// <param name="activityExceptions">The activity failures recorded by the routing slip.</param>
    /// <param name="variables">The routing-slip variables after compensation.</param>
    public RoutingSlipFaultedMessage(Guid trackingNumber, DateTimeOffset timestamp, TimeSpan duration,
        IEnumerable<ActivityException> activityExceptions, IReadOnlyDictionary<string, object> variables)
    {
        RoutingSlipMessageState.Validate(trackingNumber, duration);

        TrackingNumber = trackingNumber;
        Timestamp = timestamp;
        Duration = duration;

        ActivityExceptions = RoutingSlipMessageState.SnapshotExceptions(activityExceptions);
        Variables = RoutingSlipMessageState.Snapshot(variables);
    }

    /// <summary>Creates a routing-slip-faulted event containing one activity failure and no variables.</summary>
    /// <param name="trackingNumber">The routing-slip tracking number.</param>
    /// <param name="timestamp">The time when the routing slip reached its terminal fault.</param>
    /// <param name="duration">The elapsed time from routing-slip creation through the terminal fault.</param>
    /// <param name="activityException">The single activity failure to include.</param>
    public RoutingSlipFaultedMessage(Guid trackingNumber, DateTimeOffset timestamp, TimeSpan duration, ActivityException activityException)
    {
        RoutingSlipMessageState.Validate(trackingNumber, duration);
        ArgumentNullException.ThrowIfNull(activityException);

        Timestamp = timestamp;
        Duration = duration;

        TrackingNumber = trackingNumber;
        ActivityExceptions = RoutingSlipMessageState.SnapshotExceptions([activityException]);
        Variables = RoutingSlipMessageState.Snapshot(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>Gets or sets the routing-slip tracking number.</summary>
    public Guid TrackingNumber { get; set; }
    /// <summary>Gets or sets the time when the routing slip reached its terminal fault.</summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>Gets or sets the elapsed time from routing-slip creation through the terminal fault.</summary>
    public TimeSpan Duration { get; set; }
    /// <summary>Gets or sets the activity failures recorded by the routing slip.</summary>
    public IReadOnlyList<ActivityException> ActivityExceptions { get; set; } = null!;
    /// <summary>Gets or sets the routing-slip variables after compensation.</summary>
    public IReadOnlyDictionary<string, object> Variables { get; set; } = null!;
}
