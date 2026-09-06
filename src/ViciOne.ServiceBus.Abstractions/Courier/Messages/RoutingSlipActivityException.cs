using System;
using System.Runtime.Serialization;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Messages;

/// <summary>
/// Represents an error related to routing slip activity.
/// </summary>
public class RoutingSlipActivityException :
    ActivityException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public RoutingSlipActivityException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="activityName">The activity name value.</param>
    /// <param name="host">The host value.</param>
    /// <param name="executionId">The execution id value.</param>
    /// <param name="timestamp">The timestamp value.</param>
    /// <param name="elapsed">The elapsed value.</param>
    /// <param name="exceptionInfo">The exception info value.</param>
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

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="activityException">The activity exception value.</param>
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

    /// <summary>
    /// Gets or sets the execution id value.
    /// </summary>
    public Guid ExecutionId { get; set; }
    /// <summary>
    /// Gets or sets the timestamp value.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>
    /// Gets or sets the elapsed value.
    /// </summary>
    public TimeSpan Elapsed { get; set; }
    /// <summary>
    /// Gets or sets the name value.
    /// </summary>
    public string Name { get; set; } = null!;
    /// <summary>
    /// Gets or sets the host value.
    /// </summary>
    public HostInfo Host { get; set; } = null!;
    /// <summary>
    /// Gets or sets the exception info value.
    /// </summary>
    public ExceptionInfo ExceptionInfo { get; set; } = null!;
}
