// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Contracts.JobService
{
    public enum ConcurrentLimitKind
    {
        Configured = 0,
        Override = 1,
        Heartbeat = 2,
        Stopped = 3
    }
}
