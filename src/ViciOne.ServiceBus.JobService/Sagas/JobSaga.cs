using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Persists the lifecycle, schedule, progress, and checkpoint of one submitted job.</summary>
public sealed class JobSaga :
    ISagaStateMachineInstance,
    ISagaVersion
{
    /// <summary>Gets or sets the persisted state-machine ordinal.</summary>
    public int CurrentState { get; set; }

    /// <summary>Gets or sets when the job was submitted.</summary>
    public DateTimeOffset? Submitted { get; set; }
    /// <summary>Gets or sets the address that receives completion and failure notifications.</summary>
    public Uri ServiceAddress { get; set; } = null!;
    /// <summary>Gets or sets the maximum duration reserved for an execution slot.</summary>
    public TimeSpan? JobTimeout { get; set; }
    /// <summary>Gets or sets the serialized job payload.</summary>
    public Dictionary<string, object> Job { get; set; } = [];
    /// <summary>Gets or sets the stable identifier of the job's consumer type and endpoint.</summary>
    public Guid JobTypeId { get; set; }

    /// <summary>Gets or sets the identifier of the current execution generation.</summary>
    public Guid AttemptId { get; set; }
    /// <summary>Gets or sets the zero-based execution attempt number.</summary>
    public int RetryAttempt { get; set; }

    /// <summary>Gets or sets when the current attempt started.</summary>
    public DateTimeOffset? Started { get; set; }

    /// <summary>Gets or sets when the job most recently completed successfully.</summary>
    public DateTimeOffset? Completed { get; set; }
    /// <summary>Gets or sets the duration reported by the completed attempt.</summary>
    public TimeSpan? Duration { get; set; }

    /// <summary>Gets or sets when the job most recently faulted or was canceled.</summary>
    public DateTimeOffset? Faulted { get; set; }
    /// <summary>Gets or sets the terminal failure or cancellation reason.</summary>
    public string? Reason { get; set; }
    /// <summary>Gets or sets the scheduler token for slot allocation or the next recurrence.</summary>
    public Guid? JobSlotWaitToken { get; set; }
    /// <summary>Gets or sets the scheduler token for a delayed retry.</summary>
    public Guid? JobRetryDelayToken { get; set; }

    /// <summary>Gets or sets attempt identifiers whose supervision sagas still require finalization.</summary>
    public List<Guid>? IncompleteAttempts { get; set; }
    /// <summary>Gets or sets the last accepted progress value.</summary>
    public long? LastProgressValue { get; set; }

    /// <summary>Gets or sets the optional upper bound associated with the progress value.</summary>
    public long? LastProgressLimit { get; set; }

    /// <summary>Gets or sets the sequence number of the last accepted progress update.</summary>
    public long? LastProgressSequenceNumber { get; set; }

    /// <summary>Gets or sets the durable checkpoint saved by the current or a previous attempt.</summary>
    public Dictionary<string, object>? Checkpoint { get; set; }
    /// <summary>Gets or sets metadata supplied with the job submission.</summary>
    public Dictionary<string, object> JobProperties { get; set; } = [];
    /// <summary>Gets or sets the cron expression used to calculate recurring executions.</summary>
    public string? CronExpression { get; set; }
    /// <summary>Gets or sets the time-zone identifier used to evaluate the cron expression.</summary>
    public string? TimeZoneId { get; set; }
    /// <summary>Gets or sets the earliest permitted execution time.</summary>
    public DateTimeOffset? StartDate { get; set; }

    /// <summary>Gets or sets the end of the recurrence window.</summary>
    public DateTimeOffset? EndDate { get; set; }

    /// <summary>Gets or sets the next calculated execution time.</summary>
    public DateTimeOffset? NextStartDate { get; set; }

    /// <summary>Gets or sets the provider-specific optimistic concurrency token.</summary>
    public byte[] RowVersion { get; set; } = null!;
    /// <summary>Gets or sets the saga revision used for optimistic concurrency.</summary>
    public int Version { get; set; }

    /// <summary>Gets or sets the job identifier used to correlate lifecycle messages.</summary>
    public Guid CorrelationId { get; set; }
}
