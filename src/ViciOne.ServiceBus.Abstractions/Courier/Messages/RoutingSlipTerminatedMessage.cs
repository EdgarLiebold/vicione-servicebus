using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Messages;

/// <summary>Carries routing slip terminated message data.</summary>
public class RoutingSlipTerminatedMessage :
    RoutingSlipTerminated
{
    /// <summary>Initializes a new instance.</summary>
    public RoutingSlipTerminatedMessage()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="host">The host.</param>
    /// <param name="trackingNumber">The tracking number.</param>
    /// <param name="activityName">The activity name.</param>
    /// <param name="executionId">The execution id.</param>
    /// <param name="timestamp">The timestamp.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="variables">The variables.</param>
    /// <param name="discardedItinerary">The discarded itinerary.</param>
    public RoutingSlipTerminatedMessage(HostInfo host, Guid trackingNumber, string activityName, Guid executionId, DateTimeOffset timestamp, TimeSpan duration,
        IDictionary<string, object> variables, IEnumerable<Activity> discardedItinerary)
    {
        Host = host;
        Duration = duration;
        Timestamp = timestamp;

        TrackingNumber = trackingNumber;
        ActivityName = activityName;
        Variables = variables;
        DiscardedItinerary = discardedItinerary.ToArray();
        ExecutionId = executionId;
    }

    /// <summary>Gets or sets the tracking number.</summary>
    public Guid TrackingNumber { get; set; }
    /// <summary>Gets or sets the timestamp.</summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>Gets or sets the duration.</summary>
    public TimeSpan Duration { get; set; }

    /// <summary>Gets or sets the activity name.</summary>
    public string ActivityName { get; set; } = null!;
    /// <summary>Gets or sets the execution id.</summary>
    public Guid ExecutionId { get; set; }
    /// <summary>Gets or sets the host.</summary>
    public HostInfo Host { get; set; } = null!;

    /// <summary>Gets or sets the variables.</summary>
    public IDictionary<string, object> Variables { get; set; } = null!;
    /// <summary>Gets or sets the discarded itinerary.</summary>
    public Activity[] DiscardedItinerary { get; set; } = null!;
}
