namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>
/// Specifies the available concurrent limit kind values.
/// </summary>
public enum ConcurrentLimitKind
{
    /// <summary>
    /// Indicates configured.
    /// </summary>
    Configured = 0,
    /// <summary>
    /// Indicates override.
    /// </summary>
    Override = 1,
    /// <summary>
    /// Indicates heartbeat.
    /// </summary>
    Heartbeat = 2,
    /// <summary>
    /// Indicates stopped.
    /// </summary>
    Stopped = 3
}
