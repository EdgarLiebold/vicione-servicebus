// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Contracts.JobService;

public static class JobCancellationReasons
{
    public static readonly string Shutdown = "Job Service Shutdown";
    public static readonly string CancellationRequested = "Cancellation Requested";
    public static readonly string ConsumerInitiated = "Consumer Initiated";
}
