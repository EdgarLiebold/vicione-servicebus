using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Messages;

[Serializable]
public class RoutingSlipActivityCompletedMessage :
    RoutingSlipActivityCompleted
{
    public RoutingSlipActivityCompletedMessage()
    {
    }

    public RoutingSlipActivityCompletedMessage(HostInfo host, Guid trackingNumber, string activityName, Guid executionId,
        DateTimeOffset timestamp, TimeSpan duration, IDictionary<string, object> variables, IDictionary<string, object> arguments,
        IDictionary<string, object> data)
    {
        Host = host;
        Timestamp = timestamp;
        Duration = duration;

        TrackingNumber = trackingNumber;
        ExecutionId = executionId;
        ActivityName = activityName;
        Data = data;
        Variables = variables;
        Arguments = arguments;
    }

    public Guid TrackingNumber { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public TimeSpan Duration { get; set; }
    public Guid ExecutionId { get; set; }
    public string ActivityName { get; set; } = null!;
    public HostInfo Host { get; set; } = null!;
    public IDictionary<string, object> Arguments { get; set; } = null!;
    public IDictionary<string, object> Data { get; set; } = null!;
    public IDictionary<string, object> Variables { get; set; } = null!;
}
