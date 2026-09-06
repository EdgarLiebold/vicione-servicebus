using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures persistence coordination, supervision, retry, and scheduling behavior for jobs.</summary>
public sealed class JobSagaOptions :
    JobSagaSettingsConfigurator,
    ISpecification
{
    Uri? _jobAttemptSagaEndpointAddress;
    Uri? _jobSagaEndpointAddress;
    Uri? _jobTypeSagaEndpointAddress;

    /// <summary>Creates options with production-safe supervision, concurrency, and finalization defaults.</summary>
    public JobSagaOptions()
    {
        StatusCheckInterval = TimeSpan.FromMinutes(1);
        SlotWaitTime = TimeSpan.FromSeconds(30);
        HeartbeatTimeout = TimeSpan.FromMinutes(5);

        SuspectJobRetryCount = 3;
        ConcurrentMessageLimit = 16;
        FinalizeCompleted = true;
    }

    /// <summary>Gets or sets the maximum number of messages processed concurrently by each job saga endpoint.</summary>
    public int? ConcurrentMessageLimit { get; set; }

    IEnumerable<ValidationResult> ISpecification.Validate()
    {
        if (SlotWaitTime < TimeSpan.FromSeconds(1))
            yield return this.Failure(nameof(SlotWaitTime), "must be >= 1 second");
        if (StatusCheckInterval < TimeSpan.FromSeconds(30))
            yield return this.Failure(nameof(StatusCheckInterval), "must be >= 30 seconds");
        if (HeartbeatTimeout <= TimeSpan.Zero)
            yield return this.Failure(nameof(HeartbeatTimeout), "must be > TimeSpan.Zero");
        if (ConcurrentMessageLimit is <= 0)
            yield return this.Failure(nameof(ConcurrentMessageLimit), "must be > 0 when specified");
        if (SuspectJobRetryCount < 0)
            yield return this.Failure(nameof(SuspectJobRetryCount), "must not be negative");
        if (SuspectJobRetryDelay is TimeSpan retryDelay && retryDelay <= TimeSpan.Zero)
            yield return this.Failure(nameof(SuspectJobRetryDelay), "must be > TimeSpan.Zero when specified");
    }

    Uri JobSagaSettingsConfigurator.JobSagaEndpointAddress
    {
        set => _jobSagaEndpointAddress = value;
    }

    Uri JobSagaSettingsConfigurator.JobTypeSagaEndpointAddress
    {
        set => _jobTypeSagaEndpointAddress = value;
    }

    Uri JobSagaSettingsConfigurator.JobAttemptSagaEndpointAddress
    {
        set => _jobAttemptSagaEndpointAddress = value;
    }

    Uri JobSagaSettings.JobAttemptSagaEndpointAddress => _jobAttemptSagaEndpointAddress
        ?? throw new InvalidOperationException("The job-attempt endpoint has not been configured.");
    Uri JobSagaSettings.JobTypeSagaEndpointAddress => _jobTypeSagaEndpointAddress
        ?? throw new InvalidOperationException("The job-type endpoint has not been configured.");
    Uri JobSagaSettings.JobSagaEndpointAddress => _jobSagaEndpointAddress
        ?? throw new InvalidOperationException("The job endpoint has not been configured.");

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
