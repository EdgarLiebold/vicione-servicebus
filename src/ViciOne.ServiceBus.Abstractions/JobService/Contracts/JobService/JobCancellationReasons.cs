namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>
/// Provides a job cancellation reasons implementation.
/// </summary>
public static class JobCancellationReasons
{
    /// <summary>
    /// Defines the shutdown value.
    /// </summary>
    public static readonly string Shutdown = "Job Service Shutdown";
    /// <summary>
    /// Defines the cancellation requested value.
    /// </summary>
    public static readonly string CancellationRequested = "Cancellation Requested";
    /// <summary>
    /// Defines the consumer initiated value.
    /// </summary>
    public static readonly string ConsumerInitiated = "Consumer Initiated";
}
