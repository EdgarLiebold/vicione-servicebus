using System;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Carries the command for finalize job.</summary>
public class FinalizeJobCommand :
    FinalizeJob
{
    /// <summary>Gets or sets the job id.</summary>
    public Guid JobId { get; set; }
}
