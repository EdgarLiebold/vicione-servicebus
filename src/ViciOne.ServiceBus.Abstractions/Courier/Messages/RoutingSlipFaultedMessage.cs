using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Messages;

[Serializable]
public class RoutingSlipFaultedMessage :
    RoutingSlipFaulted
{
    public RoutingSlipFaultedMessage()
    {
    }

    public RoutingSlipFaultedMessage(Guid trackingNumber, DateTimeOffset timestamp, TimeSpan duration,
        IEnumerable<ActivityException> activityExceptions, IDictionary<string, object> variables)
    {
        TrackingNumber = trackingNumber;
        Timestamp = timestamp;
        Duration = duration;

        ActivityExceptions = activityExceptions.ToArray();
        Variables = variables;
    }

    public RoutingSlipFaultedMessage(Guid trackingNumber, DateTimeOffset timestamp, TimeSpan duration, ActivityException activityException)
    {
        Timestamp = timestamp;
        Duration = duration;

        TrackingNumber = trackingNumber;
        ActivityExceptions = new[] { activityException };
        Variables = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    }

    public Guid TrackingNumber { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public TimeSpan Duration { get; set; }
    public ActivityException[] ActivityExceptions { get; set; } = null!;
    public IDictionary<string, object> Variables { get; set; } = null!;
}
