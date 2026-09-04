using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Messages;

[Serializable]
public class RoutingSlipCompletedMessage :
    RoutingSlipCompleted
{
    public RoutingSlipCompletedMessage()
    {
    }

    public RoutingSlipCompletedMessage(Guid trackingNumber, DateTimeOffset timestamp, TimeSpan duration, IDictionary<string, object> variables)
    {
        Duration = duration;
        Timestamp = timestamp;

        TrackingNumber = trackingNumber;
        Variables = variables;
    }

    public Guid TrackingNumber { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public TimeSpan Duration { get; set; }
    public IDictionary<string, object> Variables { get; set; } = null!;
}
