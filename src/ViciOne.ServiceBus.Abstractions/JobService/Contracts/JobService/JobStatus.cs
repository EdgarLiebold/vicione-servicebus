namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Specifies the available job status values.</summary>
public enum JobStatus
{
    /// <summary>Indicates running.</summary>
    Running,
    /// <summary>Indicates faulted.</summary>
    Faulted,
    /// <summary>Indicates completed.</summary>
    Completed,
    /// <summary>Indicates canceled.</summary>
    Canceled
}
