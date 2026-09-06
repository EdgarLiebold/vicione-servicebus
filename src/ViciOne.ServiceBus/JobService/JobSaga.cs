using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Individual turnout jobs are tracked by this state.</summary>
public class JobSaga :
    SagaStateMachineInstance,
    ISagaVersion
{
    /// <summary>Gets or sets the current state.</summary>
    public int CurrentState { get; set; }

    /// <summary>Gets or sets the submitted.</summary>
    public DateTimeOffset? Submitted { get; set; }
    /// <summary>Gets or sets the service address.</summary>
    public Uri ServiceAddress { get; set; } = null!;
    /// <summary>Gets or sets the job timeout.</summary>
    public TimeSpan? JobTimeout { get; set; }
    /// <summary>Gets or sets the job.</summary>
    public Dictionary<string, object> Job { get; set; } = [];
    /// <summary>Gets or sets the job type id.</summary>
    public Guid JobTypeId { get; set; }

    /// <summary>Gets or sets the attempt id.</summary>
    public Guid AttemptId { get; set; }
    /// <summary>Gets or sets the retry attempt.</summary>
    public int RetryAttempt { get; set; }

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
    /// <summary>Gets or sets the job slot wait token.</summary>
    public Guid? JobSlotWaitToken { get; set; }
    /// <summary>Gets or sets the job retry delay token.</summary>
    public Guid? JobRetryDelayToken { get; set; }

    /// <summary>If present, keeps track of any previously faulted attempts so that the faulted job attempt saga instances can be removed when finalized.</summary>
    public List<Guid>? IncompleteAttempts { get; set; }
    /// <summary>If present, the last reported progress value.</summary>
    public long? LastProgressValue { get; set; }

    /// <summary>If present, the maximum value (can be used to show a percentage).</summary>
    public long? LastProgressLimit { get; set; }

    /// <summary>The last reported sequence number for the current job attempt.</summary>
    public long? LastProgressSequenceNumber { get; set; }

    /// <summary>The job state, saved from a previous job attempt.</summary>
    public Dictionary<string, object>? JobState { get; set; }
    /// <summary>The job properties, supplied by the submitted job.</summary>
    public Dictionary<string, object> JobProperties { get; set; } = [];
    /// <summary>For recurring jobs, the cron expression used to determine the next start date after the job has completed.</summary>
    public string? CronExpression { get; set; }
    /// <summary>The time zone for the cron expression.</summary>
    public string? TimeZoneId { get; set; }
    /// <summary>If a state date is specified, the job won't start until after the start date.</summary>
    public DateTimeOffset? StartDate { get; set; }

    /// <summary>For recurring jobs, if the <see cref="NextStartDate"/> is after the end date the job will be completed.</summary>
    public DateTimeOffset? EndDate { get; set; }

    /// <summary>For recurring jobs, the next start date based on the cron expression (and <see cref="StartDate"/>, if specified).</summary>
    public DateTimeOffset? NextStartDate { get; set; }

    /// <summary>Gets or sets the row version.</summary>
    public byte[] RowVersion { get; set; } = null!;
    /// <summary>Gets or sets the version.</summary>
    public int Version { get; set; }

    /// <summary>Gets or sets the correlation id.</summary>
    public Guid CorrelationId { get; set; }
}
