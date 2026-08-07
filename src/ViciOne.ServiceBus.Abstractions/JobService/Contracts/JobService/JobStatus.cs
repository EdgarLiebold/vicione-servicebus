// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Contracts.JobService
{
    public enum JobStatus
    {
        Running,
        Faulted,
        Completed,
        Canceled
    }
}
