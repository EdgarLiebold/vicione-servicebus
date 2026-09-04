using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Messages;

[Serializable]
public class RoutingSlipTerminatedMessage :
    RoutingSlipTerminated
{
    public RoutingSlipTerminatedMessage()
    {
    }

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

    public Guid TrackingNumber { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public TimeSpan Duration { get; set; }

    public string ActivityName { get; set; } = null!;
    public Guid ExecutionId { get; set; }
    public HostInfo Host { get; set; } = null!;

    public IDictionary<string, object> Variables { get; set; } = null!;
    public Activity[] DiscardedItinerary { get; set; } = null!;
}
