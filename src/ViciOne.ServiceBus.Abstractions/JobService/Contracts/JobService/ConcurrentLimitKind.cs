namespace ViciOne.ServiceBus.Contracts.JobService;

public enum ConcurrentLimitKind
{
    Configured = 0,
    Override = 1,
    Heartbeat = 2,
    Stopped = 3
}
