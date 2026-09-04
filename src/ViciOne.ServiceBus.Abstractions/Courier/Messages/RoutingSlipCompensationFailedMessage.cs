using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Messages;

[Serializable]
public class RoutingSlipCompensationFailedMessage :
    RoutingSlipCompensationFailed
{
    public RoutingSlipCompensationFailedMessage()
    {
    }

    public RoutingSlipCompensationFailedMessage(HostInfo host, Guid trackingNumber, DateTime failureTimestamp, TimeSpan routingSlipDuration,
        ExceptionInfo exceptionInfo,
        IDictionary<string, object> variables)
    {
        Timestamp = failureTimestamp;
        Duration = routingSlipDuration;
        Host = host;

        TrackingNumber = trackingNumber;
        Variables = variables;
        ExceptionInfo = exceptionInfo;
    }

    public Guid TrackingNumber { get; set; }
    public ExceptionInfo ExceptionInfo { get; set; } = null!;
    public IDictionary<string, object> Variables { get; set; } = null!;
    public HostInfo Host { get; set; } = null!;
    public DateTime Timestamp { get; set; }
    public TimeSpan Duration { get; set; }
}
