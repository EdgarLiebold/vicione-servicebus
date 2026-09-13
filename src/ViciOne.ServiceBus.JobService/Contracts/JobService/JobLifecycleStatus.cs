namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Identifies the lifecycle phase reported for a submitted job.</summary>
public enum JobLifecycleStatus
{
    /// <summary>The coordinator has not established a lifecycle phase.</summary>
    Unknown = 0,
    /// <summary>No persisted job exists for the requested identifier.</summary>
    NotFound = 1,
    /// <summary>The job has been submitted and is being initialized.</summary>
    Submitted = 2,
    /// <summary>The job is waiting until capacity or its scheduled start becomes available.</summary>
    WaitingForSlot = 3,
    /// <summary>The coordinator is requesting an execution slot.</summary>
    AllocatingSlot = 4,
    /// <summary>The coordinator has allocated a slot and is starting an execution attempt.</summary>
    Starting = 5,
    /// <summary>A consumer is executing the current attempt.</summary>
    Running = 6,
    /// <summary>The job is waiting for a configured retry delay.</summary>
    WaitingToRetry = 7,
    /// <summary>The most recent execution completed successfully.</summary>
    Completed = 8,
    /// <summary>The job ended because execution faulted.</summary>
    Faulted = 9,
    /// <summary>The job ended because cancellation was requested.</summary>
    Canceled = 10,
    /// <summary>Cancellation is waiting for an outstanding slot request to settle.</summary>
    CancellationPending = 11,
}
