using System;
using ViciOne.ServiceBus.Contracts.JobService;

#nullable enable
namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>
/// Provides a cancel job attempt command implementation.
/// </summary>
public class CancelJobAttemptCommand :
    CancelJobAttempt
{
    /// <summary>
    /// Gets or sets the job id value.
    /// </summary>
    public Guid JobId { get; set; }
    /// <summary>
    /// Gets or sets the attempt id value.
    /// </summary>
    public Guid AttemptId { get; set; }
    /// <summary>
    /// Gets or sets the reason value.
    /// </summary>
    public string? Reason { get; set; }
}
