using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Messages;

[Serializable]
public class RoutingSlipRevisedMessage :
    RoutingSlipRevised
{
    public RoutingSlipRevisedMessage()
    {
    }

    public RoutingSlipRevisedMessage(HostInfo host, Guid trackingNumber, string activityName, Guid executionId, DateTimeOffset timestamp, TimeSpan duration,
        IDictionary<string, object> variables,
        IEnumerable<Activity> itinerary, IEnumerable<Activity> discardedItinerary)
    {
        Host = host;
        ActivityName = activityName;
        TrackingNumber = trackingNumber;
        Timestamp = timestamp;
        Duration = duration;
        ExecutionId = executionId;
        Variables = variables;
        Itinerary = itinerary.ToArray();
        DiscardedItinerary = discardedItinerary.ToArray();
    }

    public Guid TrackingNumber { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public TimeSpan Duration { get; set; }
    public string ActivityName { get; set; } = null!;
    public Guid ExecutionId { get; set; }
    public HostInfo Host { get; set; } = null!;
    public IDictionary<string, object> Variables { get; set; } = null!;
    public Activity[] Itinerary { get; set; } = null!;
    public Activity[] DiscardedItinerary { get; set; } = null!;
}
