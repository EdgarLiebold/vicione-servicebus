using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Provides the serializable durable state snapshot returned for a job.</summary>
internal sealed class JobStateResponse :
    JobState
{
    public Guid JobId { get; set; }
    public DateTimeOffset? Submitted { get; set; }
    public DateTimeOffset? Started { get; set; }
    public DateTimeOffset? Completed { get; set; }
    public TimeSpan? Duration { get; set; }
    public DateTimeOffset? Faulted { get; set; }
    public string? Reason { get; set; }
    public int LastRetryAttempt { get; set; }
    public JobLifecycleStatus Status { get; set; }
    public long? ProgressValue { get; set; }
    public long? ProgressLimit { get; set; }
    public IReadOnlyDictionary<string, object>? Checkpoint { get; set; }
    public DateTimeOffset? NextStartDate { get; set; }
    public bool IsRecurring { get; set; }
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
}


/// <summary>Projects a serialized job state snapshot as a typed result.</summary>
/// <typeparam name="TCheckpoint">The checkpoint contract type.</typeparam>
internal sealed class JobStateResponse<TCheckpoint> :
    JobState<TCheckpoint>
    where TCheckpoint : class
{
    readonly JobState _jobState;
    readonly TCheckpoint? _typedCheckpoint;

    /// <summary>Initializes a typed projection over a serialized state snapshot.</summary>
    /// <param name="jobState">The job state.</param>
    /// <param name="typedCheckpoint">The deserialized checkpoint, or <see langword="null" /> when no checkpoint exists.</param>
    public JobStateResponse(JobState jobState, TCheckpoint? typedCheckpoint = null)
    {
        _jobState = jobState;
        _typedCheckpoint = typedCheckpoint;
    }

    public Guid JobId => _jobState.JobId;
    public DateTimeOffset? Submitted => _jobState.Submitted;
    public DateTimeOffset? Started => _jobState.Started;
    public DateTimeOffset? Completed => _jobState.Completed;
    public TimeSpan? Duration => _jobState.Duration;
    public DateTimeOffset? Faulted => _jobState.Faulted;
    public string? Reason => _jobState.Reason;
    public int LastRetryAttempt => _jobState.LastRetryAttempt;
    public JobLifecycleStatus Status => _jobState.Status;
    public long? ProgressValue => _jobState.ProgressValue;
    public long? ProgressLimit => _jobState.ProgressLimit;
    public IReadOnlyDictionary<string, object>? Checkpoint => _jobState.Checkpoint;
    public DateTimeOffset? NextStartDate => _jobState.NextStartDate;
    public bool IsRecurring => _jobState.IsRecurring;
    public DateTimeOffset? StartDate => _jobState.StartDate;
    public DateTimeOffset? EndDate => _jobState.EndDate;
    TCheckpoint? JobState<TCheckpoint>.Checkpoint => _typedCheckpoint;
}
