using System;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>
/// Provides a finalize job command implementation.
/// </summary>
public class FinalizeJobCommand :
    FinalizeJob
{
    /// <summary>
    /// Gets or sets the job id value.
    /// </summary>
    public Guid JobId { get; set; }
}
