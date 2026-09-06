namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Identifies how a concurrency update changes coordinator state for a job-service instance.</summary>
public enum JobConcurrencyUpdateKind
{
    /// <summary>Publishes configured capacity and persistent job-type metadata.</summary>
    Configuration = 0,
    /// <summary>Temporarily replaces the configured capacity.</summary>
    TemporaryOverride = 1,
    /// <summary>Renews the liveness lease without replacing persistent configuration.</summary>
    Heartbeat = 2,
    /// <summary>Removes a stopped service instance from capacity allocation.</summary>
    InstanceStopped = 3
}
