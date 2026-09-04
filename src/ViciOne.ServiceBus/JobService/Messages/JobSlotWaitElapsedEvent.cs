using System;
using ViciOne.ServiceBus.Contracts.JobService;

#nullable enable
namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>
/// Provides a job slot wait elapsed event implementation.
/// </summary>
public class JobSlotWaitElapsedEvent :
    JobSlotWaitElapsed
{
    /// <summary>
    /// Gets or sets the job id value.
    /// </summary>
    public Guid JobId { get; set; }
}
