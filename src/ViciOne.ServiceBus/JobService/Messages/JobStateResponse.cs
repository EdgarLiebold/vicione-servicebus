using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Contracts.JobService;

#nullable enable
namespace ViciOne.ServiceBus.JobService.Messages;

public class JobStateResponse :
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
    public string CurrentState { get; set; } = null!;
    public long? ProgressValue { get; set; }
    public long? ProgressLimit { get; set; }
    public Dictionary<string, object>? JobState { get; set; }
    public DateTimeOffset? NextStartDate { get; set; }
    public bool IsRecurring { get; set; }
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
}


public class JobStateResponse<T> :
    JobState<T>
    where T : class
{
    readonly JobState _jobState;
    readonly T? _jobStateOfT;

    public JobStateResponse(JobState jobState, T? jobStateOfT = null)
    {
        _jobState = jobState;
        _jobStateOfT = jobStateOfT;
    }

    public Guid JobId => _jobState.JobId;
    public DateTimeOffset? Submitted => _jobState.Submitted;
    public DateTimeOffset? Started => _jobState.Started;
    public DateTimeOffset? Completed => _jobState.Completed;
    public TimeSpan? Duration => _jobState.Duration;
    public DateTimeOffset? Faulted => _jobState.Faulted;
    public string? Reason => _jobState.Reason;
    public int LastRetryAttempt => _jobState.LastRetryAttempt;
    public string CurrentState => _jobState.CurrentState;
    public long? ProgressValue => _jobState.ProgressValue;
    public long? ProgressLimit => _jobState.ProgressLimit;
    public Dictionary<string, object>? JobState => _jobState.JobState;
    public DateTimeOffset? NextStartDate => _jobState.NextStartDate;
    public bool IsRecurring => _jobState.IsRecurring;
    public DateTimeOffset? StartDate => _jobState.StartDate;
    public DateTimeOffset? EndDate => _jobState.EndDate;
    T? JobState<T>.JobState => _jobStateOfT;
}
