namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Specifies the available job slot disposition values.</summary>
public enum JobSlotDisposition
{
    /// <summary>Indicates completed.</summary>
    Completed = 0,
    /// <summary>Indicates faulted.</summary>
    Faulted = 1,
    /// <summary>Indicates canceled.</summary>
    Canceled = 2,
    /// <summary>Indicates suspect.</summary>
    Suspect = 3,
}
