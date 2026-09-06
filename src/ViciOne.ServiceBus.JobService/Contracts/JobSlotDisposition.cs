namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Describes why an allocated job slot was released.</summary>
public enum JobSlotDisposition
{
    /// <summary>The job completed successfully.</summary>
    Completed = 0,
    /// <summary>The job ended in a terminal failure.</summary>
    Faulted = 1,
    /// <summary>The job acknowledged cancellation.</summary>
    Canceled = 2,
    /// <summary>The assigned service instance stopped proving liveness.</summary>
    Suspect = 3,
}
