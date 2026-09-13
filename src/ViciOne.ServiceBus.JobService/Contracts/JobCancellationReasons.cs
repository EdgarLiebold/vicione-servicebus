namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Defines the canonical reason strings used when jobs are canceled.</summary>
public static class JobCancellationReasons
{
    /// <summary>Indicates that the owning service instance is shutting down.</summary>
    public const string Shutdown = "Job Service Shutdown";
    /// <summary>Indicates an explicit cancellation request.</summary>
    public const string CancellationRequested = "Cancellation Requested";
    /// <summary>Indicates that the job consumer stopped its own execution.</summary>
    public const string ConsumerInitiated = "Consumer Initiated";
}
