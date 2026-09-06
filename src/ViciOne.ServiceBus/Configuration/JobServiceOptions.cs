using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.JobService;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines configuration options for job service.</summary>
public sealed class JobServiceOptions :
    JobSagaSettings,
    IOptions,
    ISpecification
{
    string _jobAttemptSagaEndpointName = null!;
    string _jobSagaEndpointName = null!;
    string _jobTypeSagaEndpointName = null!;

    /// <summary>Initializes a new instance.</summary>
    public JobServiceOptions()
    {
        StatusCheckInterval = TimeSpan.FromMinutes(1);
        SlotWaitTime = TimeSpan.FromSeconds(30);
        HeartbeatInterval = TimeSpan.FromMinutes(1);
        HeartbeatTimeout = TimeSpan.FromMinutes(5);

        SuspectJobRetryCount = 3;
        SagaPartitionCount = 16;
    }

    /// <summary>Gets or sets the job type saga endpoint name.</summary>
    public string JobTypeSagaEndpointName
    {
        get => _jobTypeSagaEndpointName;
        set
        {
            _jobTypeSagaEndpointName = value;
            JobTypeSagaEndpointAddress = new Uri($"queue:{value}");
        }
    }

    /// <summary>Gets or sets the job state saga endpoint name.</summary>
    public string JobStateSagaEndpointName
    {
        get => _jobSagaEndpointName;
        set
        {
            _jobSagaEndpointName = value;
            JobSagaEndpointAddress = new Uri($"queue:{value}");
        }
    }

    /// <summary>Gets or sets the job attempt saga endpoint name.</summary>
    public string JobAttemptSagaEndpointName
    {
        get => _jobAttemptSagaEndpointName;
        set
        {
            _jobAttemptSagaEndpointName = value;
            JobAttemptSagaEndpointAddress = new Uri($"queue:{value}");
        }
    }

    /// <summary>The job service for the endpoint.</summary>
    public IJobService JobService { get; set; } = null!;
    /// <summary>How often a job instance should send a heartbeat.</summary>
    public TimeSpan HeartbeatInterval { get; set; }

    /// <summary>
    /// If specified, overrides the default saga partition count to reduce conflicts when using optimistic concurrency.
    /// If using a saga repository with pessimistic concurrency, this is not recommended.
    /// </summary>
    public int? SagaPartitionCount { get; set; }

    /// <summary>Gets or sets the instance endpoint configurator.</summary>
    public IReceiveEndpointConfigurator InstanceEndpointConfigurator { get; set; } = null!;
    /// <summary>Gets or sets the on configure endpoint.</summary>
    public Action<IReceiveEndpointConfigurator> OnConfigureEndpoint { get; set; } = null!;
    /// <summary>Gets the concurrent message limit.</summary>
    public int? ConcurrentMessageLimit => SagaPartitionCount;

    IEnumerable<ValidationResult> ISpecification.Validate()
    {
        if (SlotWaitTime < TimeSpan.FromSeconds(1))
            yield return this.Failure(nameof(SlotWaitTime), "must be >= 1 second");
        if (StatusCheckInterval < TimeSpan.FromSeconds(30))
            yield return this.Failure(nameof(StatusCheckInterval), "must be >= 30 seconds");
        if (HeartbeatInterval <= TimeSpan.Zero)
            yield return this.Failure(nameof(HeartbeatInterval), "must be > TimeSpan.Zero");
        if (HeartbeatTimeout <= TimeSpan.Zero)
            yield return this.Failure(nameof(HeartbeatTimeout), "must be > TimeSpan.Zero");
        if (SagaPartitionCount is <= 0)
            yield return this.Failure(nameof(SagaPartitionCount), "must be > 0 when specified");
        if (SuspectJobRetryCount < 0)
            yield return this.Failure(nameof(SuspectJobRetryCount), "must not be negative");
        if (SuspectJobRetryDelay.HasValue && SuspectJobRetryDelay.Value <= TimeSpan.Zero)
            yield return this.Failure(nameof(SuspectJobRetryDelay), "must be > TimeSpan.Zero when specified");

        if (string.IsNullOrWhiteSpace(JobTypeSagaEndpointName))
            yield return this.Failure(nameof(JobTypeSagaEndpointName), "must not be null or empty");
        if (string.IsNullOrWhiteSpace(JobStateSagaEndpointName))
            yield return this.Failure(nameof(JobStateSagaEndpointName), "must not be null or empty");
        if (string.IsNullOrWhiteSpace(JobAttemptSagaEndpointName))
            yield return this.Failure(nameof(JobAttemptSagaEndpointName), "must not be null or empty");
    }

    /// <summary>The endpoint for the JobAttemptStateMachine.</summary>
    public Uri JobSagaEndpointAddress { get; set; } = null!;
    /// <summary>The endpoint for the JobAttemptStateMachine.</summary>
    public Uri JobTypeSagaEndpointAddress { get; set; } = null!;
    /// <summary>The endpoint for the JobAttemptStateMachine.</summary>
    public Uri JobAttemptSagaEndpointAddress { get; set; } = null!;
    /// <summary>The time to wait for a job slot when one is unavailable.</summary>
    public TimeSpan SlotWaitTime { get; set; }

    /// <summary>The time after which the status of a job should be checked.</summary>
    public TimeSpan StatusCheckInterval { get; set; }

    /// <summary>The time after which an instance will automatically be purged from the instance list.</summary>
    public TimeSpan HeartbeatTimeout { get; set; }

    /// <summary>The number of times to retry a suspect job before it is faulted. Defaults to zero.</summary>
    public int SuspectJobRetryCount { get; set; }

    /// <summary>The delay before retrying a suspect job.</summary>
    public TimeSpan? SuspectJobRetryDelay { get; set; }

    /// <summary>If true, completed jobs will be finalized, removing the saga from the repository.</summary>
    public bool FinalizeCompleted { get; set; }

    /// <summary>
    /// Optional resolver for platform-specific or application-defined time zone identifiers.
    /// The resolver is owned by this job-service configuration instance.
    /// </summary>
    public Func<string, TimeZoneInfo> TimeZoneResolver { get; set; } = null!;
}
