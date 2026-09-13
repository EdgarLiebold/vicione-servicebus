namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Identifies the lifecycle state observed for one job attempt.</summary>
public enum JobAttemptStatusKind
{
    /// <summary>The attempt is still running.</summary>
    Running,

    /// <summary>The attempt ended in failure.</summary>
    Faulted,

    /// <summary>The attempt completed successfully.</summary>
    Completed,

    /// <summary>The attempt acknowledged cancellation.</summary>
    Canceled
}
