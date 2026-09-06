using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Carries the response for job state.</summary>
public class JobStateResponse :
    JobState
{
    /// <summary>Gets or sets the job id.</summary>
    public Guid JobId { get; set; }
    /// <summary>Gets or sets the submitted.</summary>
    public DateTimeOffset? Submitted { get; set; }
    /// <summary>Gets or sets the started.</summary>
    public DateTimeOffset? Started { get; set; }
    /// <summary>Gets or sets the completed.</summary>
    public DateTimeOffset? Completed { get; set; }
    /// <summary>Gets or sets the duration.</summary>
    public TimeSpan? Duration { get; set; }
    /// <summary>Gets or sets the faulted.</summary>
    public DateTimeOffset? Faulted { get; set; }
    /// <summary>Gets or sets the reason.</summary>
    public string? Reason { get; set; }
    /// <summary>Gets or sets the last retry attempt.</summary>
    public int LastRetryAttempt { get; set; }
    /// <summary>Gets or sets the current state.</summary>
    public string CurrentState { get; set; } = null!;
    /// <summary>Gets or sets the progress value carried by this instance.</summary>
    public long? ProgressValue { get; set; }
    /// <summary>Gets or sets the progress limit.</summary>
    public long? ProgressLimit { get; set; }
    /// <summary>Gets or sets the job state.</summary>
    public Dictionary<string, object>? JobState { get; set; }
    /// <summary>Gets or sets the next start date.</summary>
    public DateTimeOffset? NextStartDate { get; set; }
    /// <summary>Gets or sets a value indicating whether recurring.</summary>
    public bool IsRecurring { get; set; }
    /// <summary>Gets or sets the start date.</summary>
    public DateTimeOffset? StartDate { get; set; }
    /// <summary>Gets or sets the end date.</summary>
    public DateTimeOffset? EndDate { get; set; }
}


/// <summary>Carries the response for job state.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class JobStateResponse<T> :
    JobState<T>
    where T : class
{
    readonly JobState _jobState;
    readonly T? _jobStateOfT;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="jobState">The job state.</param>
    /// <param name="jobStateOfT">The job state of t.</param>
    public JobStateResponse(JobState jobState, T? jobStateOfT = null)
    {
        _jobState = jobState;
        _jobStateOfT = jobStateOfT;
    }

    /// <summary>Gets the job id.</summary>
    public Guid JobId => _jobState.JobId;
    /// <summary>Gets the submitted.</summary>
    public DateTimeOffset? Submitted => _jobState.Submitted;
    /// <summary>Gets the started.</summary>
    public DateTimeOffset? Started => _jobState.Started;
    /// <summary>Gets the completed.</summary>
    public DateTimeOffset? Completed => _jobState.Completed;
    /// <summary>Gets the duration.</summary>
    public TimeSpan? Duration => _jobState.Duration;
    /// <summary>Gets the faulted.</summary>
    public DateTimeOffset? Faulted => _jobState.Faulted;
    /// <summary>Gets the reason.</summary>
    public string? Reason => _jobState.Reason;
    /// <summary>Gets the last retry attempt.</summary>
    public int LastRetryAttempt => _jobState.LastRetryAttempt;
    /// <summary>Gets the current state.</summary>
    public string CurrentState => _jobState.CurrentState;
    /// <summary>Gets the progress value carried by this instance.</summary>
    public long? ProgressValue => _jobState.ProgressValue;
    /// <summary>Gets the progress limit.</summary>
    public long? ProgressLimit => _jobState.ProgressLimit;
    /// <summary>Gets the job state.</summary>
    public Dictionary<string, object>? JobState => _jobState.JobState;
    /// <summary>Gets the next start date.</summary>
    public DateTimeOffset? NextStartDate => _jobState.NextStartDate;
    /// <summary>Gets a value indicating whether recurring.</summary>
    public bool IsRecurring => _jobState.IsRecurring;
    /// <summary>Gets the start date.</summary>
    public DateTimeOffset? StartDate => _jobState.StartDate;
    /// <summary>Gets the end date.</summary>
    public DateTimeOffset? EndDate => _jobState.EndDate;
    T? JobState<T>.JobState => _jobStateOfT;
}
