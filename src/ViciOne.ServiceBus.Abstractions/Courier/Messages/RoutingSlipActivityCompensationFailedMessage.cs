using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Messages;

[Serializable]
public class RoutingSlipActivityCompensationFailedMessage :
    RoutingSlipActivityCompensationFailed
{
    public RoutingSlipActivityCompensationFailedMessage()
    {
    }

    public RoutingSlipActivityCompensationFailedMessage(HostInfo host, Guid trackingNumber, string activityName, Guid executionId, DateTime timestamp,
        TimeSpan duration, ExceptionInfo exceptionInfo, IDictionary<string, object> variables, IDictionary<string, object> data)
    {
        Host = host;
        Duration = duration;
        Timestamp = timestamp;

        TrackingNumber = trackingNumber;
        ExecutionId = executionId;
        ActivityName = activityName;
        Data = data;
        Variables = variables;
        ExceptionInfo = exceptionInfo;
    }

    public Guid TrackingNumber { get; set; }
    public DateTime Timestamp { get; set; }
    public Guid ExecutionId { get; set; }
    public string ActivityName { get; set; } = null!;
    public IDictionary<string, object> Data { get; set; } = null!;
    public ExceptionInfo ExceptionInfo { get; set; } = null!;
    public IDictionary<string, object> Variables { get; set; } = null!;
    public TimeSpan Duration { get; set; }
    public HostInfo Host { get; set; } = null!;
}
