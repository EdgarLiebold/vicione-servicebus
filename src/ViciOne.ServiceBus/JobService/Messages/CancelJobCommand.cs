using System;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Carries the command for cancel job.</summary>
public class CancelJobCommand :
    CancelJob
{
    /// <summary>Gets or sets the job id.</summary>
    public Guid JobId { get; set; }
    /// <summary>Gets or sets the reason.</summary>
    public string? Reason { get; set; }
}
