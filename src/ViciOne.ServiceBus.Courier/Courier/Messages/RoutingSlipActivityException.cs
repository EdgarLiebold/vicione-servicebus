using System;
using System.Runtime.Serialization;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Messages;

/// <summary>Materializes a failure record for one routing-slip activity execution.</summary>
internal sealed class RoutingSlipActivityException :
    IActivityException
{
    /// <summary>Creates an empty instance for contract materialization.</summary>
    public RoutingSlipActivityException()
    {
    }

    /// <summary>Creates an activity-failure record.</summary>
    /// <param name="activityName">The non-empty activity name.</param>
    /// <param name="host">The host that executed the activity.</param>
    /// <param name="executionId">The non-empty activity execution identifier.</param>
    /// <param name="timestamp">The time when activity execution started.</param>
    /// <param name="elapsed">The non-negative duration before failure.</param>
    /// <param name="exceptionInfo">The captured activity failure.</param>
    public RoutingSlipActivityException(string activityName, HostInfo host, Guid executionId, DateTimeOffset timestamp, TimeSpan elapsed,
        ExceptionInfo exceptionInfo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activityName);
        ArgumentNullException.ThrowIfNull(host);
        if (executionId == Guid.Empty)
            throw new ArgumentException("The activity execution identifier cannot be empty.", nameof(executionId));
        ArgumentOutOfRangeException.ThrowIfLessThan(elapsed, TimeSpan.Zero);
        ArgumentNullException.ThrowIfNull(exceptionInfo);

        ExecutionId = executionId;

        Timestamp = timestamp;
        Elapsed = elapsed;
        Name = activityName;
        Host = host;
        ExceptionInfo = exceptionInfo;
    }

    /// <summary>Creates a validated snapshot of a received activity-failure record.</summary>
    /// <param name="activityException">The received activity failure to copy.</param>
    public RoutingSlipActivityException(IActivityException activityException)
    {
        ArgumentNullException.ThrowIfNull(activityException);

        string? name = activityException.Name;
        if (string.IsNullOrWhiteSpace(name))
            throw new SerializationException("An activity failure requires an activity name.");

        HostInfo? host = activityException.Host;
        if (host is null)
            throw new SerializationException("An activity failure requires host information.");

        Guid executionId = activityException.ExecutionId;
        if (executionId == Guid.Empty)
            throw new SerializationException("An activity failure requires a non-empty execution identifier.");

        TimeSpan elapsed = activityException.Elapsed;
        if (elapsed < TimeSpan.Zero)
            throw new SerializationException("An activity failure cannot have a negative elapsed duration.");

        ExceptionInfo? exceptionInfo = activityException.ExceptionInfo;
        if (exceptionInfo == null)
            throw new SerializationException("An activity failure requires exception information.");

        ExecutionId = executionId;
        Timestamp = activityException.Timestamp;
        Elapsed = elapsed;
        Name = name;
        Host = host;
        ExceptionInfo = exceptionInfo;
    }

    /// <summary>Gets or sets the activity execution identifier.</summary>
    public Guid ExecutionId { get; set; }
    /// <summary>Gets or sets the time when activity execution started.</summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>Gets or sets the duration before failure.</summary>
    public TimeSpan Elapsed { get; set; }
    /// <summary>Gets or sets the activity name.</summary>
    public string Name { get; set; } = null!;
    /// <summary>Gets or sets the host that executed the activity.</summary>
    public HostInfo Host { get; set; } = null!;
    /// <summary>Gets or sets the captured activity failure.</summary>
    public ExceptionInfo ExceptionInfo { get; set; } = null!;
}
