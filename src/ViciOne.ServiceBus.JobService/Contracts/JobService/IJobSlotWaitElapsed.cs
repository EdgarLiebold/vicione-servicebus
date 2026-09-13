using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Signals that a job should retry execution-slot allocation.</summary>
public interface IJobSlotWaitElapsed
{
    /// <summary>Gets the job that may attempt allocation again.</summary>
    Guid JobId { get; }
}
