using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Messages;

/// <summary>Carries routing slip faulted message data.</summary>
internal sealed class RoutingSlipFaultedMessage :
    RoutingSlipFaulted
{
    /// <summary>Initializes a new instance.</summary>
    public RoutingSlipFaultedMessage()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="trackingNumber">The tracking number.</param>
    /// <param name="timestamp">The timestamp.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="activityExceptions">The activity exceptions.</param>
    /// <param name="variables">The variables.</param>
    public RoutingSlipFaultedMessage(Guid trackingNumber, DateTimeOffset timestamp, TimeSpan duration,
        IEnumerable<ActivityException> activityExceptions, IDictionary<string, object> variables)
    {
        TrackingNumber = trackingNumber;
        Timestamp = timestamp;
        Duration = duration;

        ActivityExceptions = activityExceptions.ToArray();
        Variables = variables;
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="trackingNumber">The tracking number.</param>
    /// <param name="timestamp">The timestamp.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="activityException">The activity exception.</param>
    public RoutingSlipFaultedMessage(Guid trackingNumber, DateTimeOffset timestamp, TimeSpan duration, ActivityException activityException)
    {
        Timestamp = timestamp;
        Duration = duration;

        TrackingNumber = trackingNumber;
        ActivityExceptions = new[] { activityException };
        Variables = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Gets or sets the tracking number.</summary>
    public Guid TrackingNumber { get; set; }
    /// <summary>Gets or sets the timestamp.</summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>Gets or sets the duration.</summary>
    public TimeSpan Duration { get; set; }
    /// <summary>Gets or sets the activity exceptions.</summary>
    public ActivityException[] ActivityExceptions { get; set; } = null!;
    /// <summary>Gets or sets the variables.</summary>
    public IDictionary<string, object> Variables { get; set; } = null!;
}
