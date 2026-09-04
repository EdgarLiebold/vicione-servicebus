using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Messages;

/// <summary>
/// Provides a routing slip activity faulted message implementation.
/// </summary>
[Serializable]
public class RoutingSlipActivityFaultedMessage :
    RoutingSlipActivityFaulted
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public RoutingSlipActivityFaultedMessage()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="host">The host value.</param>
    /// <param name="trackingNumber">The tracking number value.</param>
    /// <param name="activityName">The activity name value.</param>
    /// <param name="executionId">The execution id value.</param>
    /// <param name="timestamp">The timestamp value.</param>
    /// <param name="duration">The duration value.</param>
    /// <param name="exceptionInfo">The exception info value.</param>
    /// <param name="variables">The variables value.</param>
    /// <param name="arguments">The arguments value.</param>
    public RoutingSlipActivityFaultedMessage(HostInfo host, Guid trackingNumber, string activityName, Guid executionId,
        DateTimeOffset timestamp, TimeSpan duration, ExceptionInfo exceptionInfo, IDictionary<string, object> variables,
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

    /// <summary>
    /// Gets or sets the tracking number value.
    /// </summary>
    public Guid TrackingNumber { get; set; }
    /// <summary>
    /// Gets or sets the timestamp value.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>
    /// Gets or sets the duration value.
    /// </summary>
    public TimeSpan Duration { get; set; }
    /// <summary>
    /// Gets or sets the activity name value.
    /// </summary>
    public string ActivityName { get; set; } = null!;
    /// <summary>
    /// Gets or sets the host value.
    /// </summary>
    public HostInfo Host { get; set; } = null!;
    /// <summary>
    /// Gets or sets the execution id value.
    /// </summary>
    public Guid ExecutionId { get; set; }
    /// <summary>
    /// Gets or sets the exception info value.
    /// </summary>
    public ExceptionInfo ExceptionInfo { get; set; } = null!;
    /// <summary>
    /// Gets or sets the arguments value.
    /// </summary>
    public IDictionary<string, object> Arguments { get; set; } = null!;
    /// <summary>
    /// Gets or sets the variables value.
    /// </summary>
    public IDictionary<string, object> Variables { get; set; } = null!;
}
