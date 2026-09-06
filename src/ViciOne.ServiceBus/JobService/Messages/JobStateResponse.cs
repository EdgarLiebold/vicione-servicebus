using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>
/// Provides a job state response implementation.
/// </summary>
public class JobStateResponse :
    JobState
{
    /// <summary>
    /// Gets or sets the job id value.
    /// </summary>
    public Guid JobId { get; set; }
    /// <summary>
    /// Gets or sets the submitted value.
    /// </summary>
    public DateTimeOffset? Submitted { get; set; }
    /// <summary>
    /// Gets or sets the started value.
    /// </summary>
    public DateTimeOffset? Started { get; set; }
    /// <summary>
    /// Gets or sets the completed value.
    /// </summary>
    public DateTimeOffset? Completed { get; set; }
    /// <summary>
    /// Gets or sets the duration value.
    /// </summary>
    public TimeSpan? Duration { get; set; }
    /// <summary>
    /// Gets or sets the faulted value.
    /// </summary>
    public DateTimeOffset? Faulted { get; set; }
    /// <summary>
    /// Gets or sets the reason value.
    /// </summary>
    public string? Reason { get; set; }
    /// <summary>
    /// Gets or sets the last retry attempt value.
    /// </summary>
    public int LastRetryAttempt { get; set; }
    /// <summary>
    /// Gets or sets the current state value.
    /// </summary>
    public string CurrentState { get; set; } = null!;
    /// <summary>
    /// Gets or sets the progress value value.
    /// </summary>
    public long? ProgressValue { get; set; }
    /// <summary>
    /// Gets or sets the progress limit value.
    /// </summary>
    public long? ProgressLimit { get; set; }
    /// <summary>
    /// Gets or sets the job state value.
    /// </summary>
    public Dictionary<string, object>? JobState { get; set; }
    /// <summary>
    /// Gets or sets the next start date value.
    /// </summary>
    public DateTimeOffset? NextStartDate { get; set; }
    /// <summary>
    /// Gets or sets the is recurring value.
    /// </summary>
    public bool IsRecurring { get; set; }
    /// <summary>
    /// Gets or sets the start date value.
    /// </summary>
    public DateTimeOffset? StartDate { get; set; }
    /// <summary>
    /// Gets or sets the end date value.
    /// </summary>
    public DateTimeOffset? EndDate { get; set; }
}


/// <summary>
/// Provides a job state response implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class JobStateResponse<T> :
    JobState<T>
    where T : class
{
    readonly JobState _jobState;
    readonly T? _jobStateOfT;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="jobState">The job state value.</param>
    /// <param name="jobStateOfT">The job state of t value.</param>
    public JobStateResponse(JobState jobState, T? jobStateOfT = null)
    {
        _jobState = jobState;
        _jobStateOfT = jobStateOfT;
    }

    /// <summary>
    /// Gets the job id value.
    /// </summary>
    public Guid JobId => _jobState.JobId;
    /// <summary>
    /// Gets the submitted value.
    /// </summary>
    public DateTimeOffset? Submitted => _jobState.Submitted;
    /// <summary>
    /// Gets the started value.
    /// </summary>
    public DateTimeOffset? Started => _jobState.Started;
    /// <summary>
    /// Gets the completed value.
    /// </summary>
    public DateTimeOffset? Completed => _jobState.Completed;
    /// <summary>
    /// Gets the duration value.
    /// </summary>
    public TimeSpan? Duration => _jobState.Duration;
    /// <summary>
    /// Gets the faulted value.
    /// </summary>
    public DateTimeOffset? Faulted => _jobState.Faulted;
    /// <summary>
    /// Gets the reason value.
    /// </summary>
    public string? Reason => _jobState.Reason;
    /// <summary>
    /// Gets the last retry attempt value.
    /// </summary>
    public int LastRetryAttempt => _jobState.LastRetryAttempt;
    /// <summary>
    /// Gets the current state value.
    /// </summary>
    public string CurrentState => _jobState.CurrentState;
    /// <summary>
    /// Gets the progress value value.
    /// </summary>
    public long? ProgressValue => _jobState.ProgressValue;
    /// <summary>
    /// Gets the progress limit value.
    /// </summary>
    public long? ProgressLimit => _jobState.ProgressLimit;
    /// <summary>
    /// Gets the job state value.
    /// </summary>
    public Dictionary<string, object>? JobState => _jobState.JobState;
    /// <summary>
    /// Gets the next start date value.
    /// </summary>
    public DateTimeOffset? NextStartDate => _jobState.NextStartDate;
    /// <summary>
    /// Gets the is recurring value.
    /// </summary>
    public bool IsRecurring => _jobState.IsRecurring;
    /// <summary>
    /// Gets the start date value.
    /// </summary>
    public DateTimeOffset? StartDate => _jobState.StartDate;
    /// <summary>
    /// Gets the end date value.
    /// </summary>
    public DateTimeOffset? EndDate => _jobState.EndDate;
    T? JobState<T>.JobState => _jobStateOfT;
}
