using System;
using System.Runtime.Serialization;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Messages;

[Serializable]
public class RoutingSlipActivityException :
    ActivityException
{
    public RoutingSlipActivityException()
    {
    }

    public RoutingSlipActivityException(string activityName, HostInfo host, Guid executionId, DateTimeOffset timestamp, TimeSpan elapsed,
        ExceptionInfo exceptionInfo)
    {
        ExecutionId = executionId;

        Timestamp = timestamp;
        Elapsed = elapsed;
        Name = activityName;
        Host = host;
        ExceptionInfo = exceptionInfo;
    }

    public RoutingSlipActivityException(ActivityException activityException)
    {
        if (string.IsNullOrEmpty(activityException.Name))
            throw new SerializationException("An Activity Name is required");
        if (activityException.ExceptionInfo == null)
            throw new SerializationException("An Activity ExceptionInfo is required");

        ExecutionId = activityException.ExecutionId;
        Timestamp = activityException.Timestamp;
        Elapsed = activityException.Elapsed;
        Name = activityException.Name;
        Host = activityException.Host;
        ExceptionInfo = activityException.ExceptionInfo;
    }

    public Guid ExecutionId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public TimeSpan Elapsed { get; set; }
    public string Name { get; set; } = null!;
    public HostInfo Host { get; set; } = null!;
    public ExceptionInfo ExceptionInfo { get; set; } = null!;
}
