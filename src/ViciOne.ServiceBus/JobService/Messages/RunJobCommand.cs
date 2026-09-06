using System;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Carries the command for run job.</summary>
public class RunJobCommand :
    RunJob
{
    /// <summary>Gets or sets the job id.</summary>
    public Guid JobId { get; set; }
}
