using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures job coordination endpoints and their persistence repositories.</summary>
public interface IJobServiceConfigurator
{
    /// <summary>
    /// Sets the repository that coordinates job-type capacity and active service instances.
    /// When omitted, the endpoint uses volatile in-memory persistence.
    /// </summary>
    ISagaRepository<JobTypeSaga> JobTypeRepository { set; }

    /// <summary>
    /// Sets the repository that tracks submitted jobs across their complete lifecycle.
    /// When omitted, the endpoint uses volatile in-memory persistence.
    /// </summary>
    ISagaRepository<JobSaga> JobRepository { set; }

    /// <summary>
    /// Sets the repository that supervises individual execution attempts.
    /// When omitted, the endpoint uses volatile in-memory persistence.
    /// </summary>
    ISagaRepository<JobAttemptSaga> JobAttemptRepository { set; }

    /// <summary>Sets the endpoint name for distributed job-type capacity coordination.</summary>
    string JobTypeEndpointName { set; }

    /// <summary>Sets the endpoint name for job lifecycle coordination.</summary>
    string JobEndpointName { set; }

    /// <summary>Sets the endpoint name for job-attempt supervision.</summary>
    string JobAttemptEndpointName { set; }

    /// <summary>Sets how often the local service instance announces its availability.</summary>
    TimeSpan HeartbeatInterval { set; }

    /// <summary>Sets how long a service instance may remain silent before its capacity is discarded.</summary>
    TimeSpan HeartbeatTimeout { set; }

    /// <summary>Sets the delay before a locally rejected attempt is made available for rescheduling.</summary>
    TimeSpan RejectedJobDelay { set; }

    /// <summary>Sets the clock used for local job execution and lifecycle timestamps.</summary>
    TimeProvider TimeProvider { set; }

    /// <summary>Sets the delay before capacity allocation is attempted again.</summary>
    TimeSpan SlotWaitTime { set; }

    /// <summary>Sets the interval between liveness checks for an active job attempt.</summary>
    TimeSpan StatusCheckInterval { set; }

    /// <summary>Sets the number of supervision timeouts tolerated before an attempt is faulted.</summary>
    int SuspectJobRetryCount { set; }

    /// <summary>Sets or clears the delay before repeating a failed liveness check.</summary>
    TimeSpan? SuspectJobRetryDelay { set; }

    /// <summary>Sets or clears the concurrent message limit applied to each coordination endpoint.</summary>
    int? ConcurrentMessageLimit { set; }

    /// <summary>Sets whether completed jobs are removed from persistence automatically.</summary>
    bool FinalizeCompleted { set; }

    /// <summary>
    /// Sets or clears the fallback used when the operating system cannot resolve a time-zone identifier.
    /// Returning <see langword="null" /> reports that the fallback does not recognize the identifier.
    /// </summary>
    Func<string, TimeZoneInfo?>? TimeZoneResolver { set; }
}
