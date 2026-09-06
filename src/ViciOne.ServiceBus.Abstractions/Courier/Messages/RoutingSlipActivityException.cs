using System;
using System.Runtime.Serialization;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Messages;

/// <summary>Represents an error related to routing slip activity.</summary>
public class RoutingSlipActivityException :
    ActivityException
{
    /// <summary>Initializes a new instance.</summary>
    public RoutingSlipActivityException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="activityName">The activity name.</param>
    /// <param name="host">The host.</param>
    /// <param name="executionId">The execution id.</param>
    /// <param name="timestamp">The timestamp.</param>
    /// <param name="elapsed">The elapsed.</param>
    /// <param name="exceptionInfo">The exception info.</param>
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

    /// <summary>Initializes a new instance.</summary>
    /// <param name="activityException">The activity exception.</param>
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

    /// <summary>Gets or sets the execution id.</summary>
    public Guid ExecutionId { get; set; }
    /// <summary>Gets or sets the timestamp.</summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>Gets or sets the elapsed.</summary>
    public TimeSpan Elapsed { get; set; }
    /// <summary>Gets or sets the name.</summary>
    public string Name { get; set; } = null!;
    /// <summary>Gets or sets the host.</summary>
    public HostInfo Host { get; set; } = null!;
    /// <summary>Gets or sets the exception info.</summary>
    public ExceptionInfo ExceptionInfo { get; set; } = null!;
}
