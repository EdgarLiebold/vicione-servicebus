using System;
using ViciOne.ServiceBus.Contracts.JobService;

#nullable enable
namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>
/// Provides a cancel job command implementation.
/// </summary>
public class CancelJobCommand :
    CancelJob
{
    /// <summary>
    /// Gets or sets the job id value.
    /// </summary>
    public Guid JobId { get; set; }
    /// <summary>
    /// Gets or sets the reason value.
    /// </summary>
    public string? Reason { get; set; }
}
