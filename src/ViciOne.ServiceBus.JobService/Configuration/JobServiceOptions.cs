using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.JobService;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Stores endpoint and supervision settings for direct job-service configuration.</summary>
public sealed class JobServiceOptions :
    JobSagaSettings,
    IOptions,
    ISpecification
{
    Uri? _jobAttemptSagaEndpointAddress;
    Uri? _jobSagaEndpointAddress;
    Uri? _jobTypeSagaEndpointAddress;

    /// <summary>Creates options with production-safe timing, partitioning, and finalization defaults.</summary>
    public JobServiceOptions()
    {
        StatusCheckInterval = TimeSpan.FromMinutes(1);
        SlotWaitTime = TimeSpan.FromSeconds(30);
        HeartbeatInterval = TimeSpan.FromMinutes(1);
        HeartbeatTimeout = TimeSpan.FromMinutes(5);
        RejectedJobDelay = TimeSpan.FromSeconds(3);
        TimeProvider = TimeProvider.System;

        SuspectJobRetryCount = 3;
        ConcurrentMessageLimit = 16;
        FinalizeCompleted = true;
    }

    /// <summary>Gets or sets the endpoint name for distributed job-type capacity coordination.</summary>
    public string JobTypeEndpointName { get; set; } = string.Empty;

    /// <summary>Gets or sets the endpoint name for job lifecycle coordination.</summary>
    public string JobEndpointName { get; set; } = string.Empty;

    /// <summary>Gets or sets the endpoint name for job-attempt supervision.</summary>
    public string JobAttemptEndpointName { get; set; } = string.Empty;

    /// <summary>Gets or sets how often a job-service instance announces its availability.</summary>
    public TimeSpan HeartbeatInterval { get; set; }

    /// <summary>Gets or sets the delay before a locally rejected attempt is made available for rescheduling.</summary>
    public TimeSpan RejectedJobDelay { get; set; }

    /// <summary>Gets or sets the clock used for local job execution and lifecycle timestamps.</summary>
    public TimeProvider TimeProvider { get; set; }

    /// <summary>Gets or sets the concurrent message limit applied to each coordination endpoint.</summary>
    public int? ConcurrentMessageLimit { get; set; }

    IEnumerable<ValidationResult> ISpecification.Validate()
    {
        if (SlotWaitTime < TimeSpan.FromSeconds(1))
            yield return this.Failure(nameof(SlotWaitTime), "must be >= 1 second");
        if (StatusCheckInterval < TimeSpan.FromSeconds(30))
            yield return this.Failure(nameof(StatusCheckInterval), "must be >= 30 seconds");
        if (HeartbeatInterval <= TimeSpan.Zero)
            yield return this.Failure(nameof(HeartbeatInterval), "must be > TimeSpan.Zero");
        if (RejectedJobDelay <= TimeSpan.Zero)
            yield return this.Failure(nameof(RejectedJobDelay), "must be > TimeSpan.Zero");
        if (TimeProvider == null)
            yield return this.Failure(nameof(TimeProvider), "must not be null");
        if (HeartbeatTimeout <= TimeSpan.Zero)
            yield return this.Failure(nameof(HeartbeatTimeout), "must be > TimeSpan.Zero");
        if (ConcurrentMessageLimit is <= 0)
            yield return this.Failure(nameof(ConcurrentMessageLimit), "must be > 0 when specified");
        if (SuspectJobRetryCount < 0)
            yield return this.Failure(nameof(SuspectJobRetryCount), "must not be negative");
        if (SuspectJobRetryDelay.HasValue && SuspectJobRetryDelay.Value <= TimeSpan.Zero)
            yield return this.Failure(nameof(SuspectJobRetryDelay), "must be > TimeSpan.Zero when specified");

        if (string.IsNullOrWhiteSpace(JobTypeEndpointName))
            yield return this.Failure(nameof(JobTypeEndpointName), "must not be null or empty");
        if (string.IsNullOrWhiteSpace(JobEndpointName))
            yield return this.Failure(nameof(JobEndpointName), "must not be null or empty");
        if (string.IsNullOrWhiteSpace(JobAttemptEndpointName))
            yield return this.Failure(nameof(JobAttemptEndpointName), "must not be null or empty");
    }

    internal Uri? JobSagaEndpointAddress
    {
        get => _jobSagaEndpointAddress;
        set => _jobSagaEndpointAddress = value;
    }

    internal Uri? JobTypeSagaEndpointAddress
    {
        get => _jobTypeSagaEndpointAddress;
        set => _jobTypeSagaEndpointAddress = value;
    }

    internal Uri? JobAttemptSagaEndpointAddress
    {
        get => _jobAttemptSagaEndpointAddress;
        set => _jobAttemptSagaEndpointAddress = value;
    }

    Uri JobSagaSettings.JobSagaEndpointAddress => _jobSagaEndpointAddress
        ?? throw new InvalidOperationException("The job endpoint has not been configured.");
    Uri JobSagaSettings.JobTypeSagaEndpointAddress => _jobTypeSagaEndpointAddress
        ?? throw new InvalidOperationException("The job-type endpoint has not been configured.");
    Uri JobSagaSettings.JobAttemptSagaEndpointAddress => _jobAttemptSagaEndpointAddress
        ?? throw new InvalidOperationException("The job-attempt endpoint has not been configured.");
    /// <summary>Gets or sets the delay before capacity allocation is attempted again.</summary>
    public TimeSpan SlotWaitTime { get; set; }

    /// <summary>Gets or sets the interval between liveness checks for an active job attempt.</summary>
    public TimeSpan StatusCheckInterval { get; set; }

    /// <summary>Gets or sets how long an instance may remain silent before its capacity is discarded.</summary>
    public TimeSpan HeartbeatTimeout { get; set; }

    /// <summary>Gets or sets the number of failed liveness checks tolerated before an attempt is faulted.</summary>
    public int SuspectJobRetryCount { get; set; }

    /// <summary>Gets or sets the optional delay before repeating a failed liveness check.</summary>
    public TimeSpan? SuspectJobRetryDelay { get; set; }

    /// <summary>Gets or sets whether completed jobs are removed from persistence automatically.</summary>
    public bool FinalizeCompleted { get; set; }

    /// <summary>
    /// Optional resolver for platform-specific or application-defined time zone identifiers.
    /// The resolver is owned by this job-service configuration instance.
    /// </summary>
    public Func<string, TimeZoneInfo?>? TimeZoneResolver { get; set; }
}
