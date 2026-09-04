using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Messages;

[Serializable]
public class RoutingSlipActivityFaultedMessage :
    RoutingSlipActivityFaulted
{
    public RoutingSlipActivityFaultedMessage()
    {
    }

    public RoutingSlipActivityFaultedMessage(HostInfo host, Guid trackingNumber, string activityName, Guid executionId,
        DateTime timestamp, TimeSpan duration, ExceptionInfo exceptionInfo, IDictionary<string, object> variables,
        IDictionary<string, object> arguments)
    {
        Host = host;
        TrackingNumber = trackingNumber;
        Timestamp = timestamp;
        Duration = duration;
        ExecutionId = executionId;
        ActivityName = activityName;
        Variables = variables;
        Arguments = arguments;
        ExceptionInfo = exceptionInfo;
    }

    public Guid TrackingNumber { get; set; }
    public DateTime Timestamp { get; set; }
    public TimeSpan Duration { get; set; }
    public string ActivityName { get; set; } = null!;
    public HostInfo Host { get; set; } = null!;
    public Guid ExecutionId { get; set; }
    public ExceptionInfo ExceptionInfo { get; set; } = null!;
    public IDictionary<string, object> Arguments { get; set; } = null!;
    public IDictionary<string, object> Variables { get; set; } = null!;
}
