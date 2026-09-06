namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Defines the canonical reason strings used when jobs are cancelled.</summary>
public static class JobCancellationReasons
{
    /// <summary>Exposes the shutdown used by the containing type.</summary>
    public static readonly string Shutdown = "Job Service Shutdown";
    /// <summary>Exposes the cancellation requested used by the containing type.</summary>
    public static readonly string CancellationRequested = "Cancellation Requested";
    /// <summary>Exposes the consumer initiated used by the containing type.</summary>
    public static readonly string ConsumerInitiated = "Consumer Initiated";
}
