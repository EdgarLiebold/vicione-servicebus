using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Contracts.JobService;

#nullable enable
namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>
/// Provides a save job state command implementation.
/// </summary>
public class SaveJobStateCommand :
    SaveJobState
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
    /// Gets or sets the job state value.
    /// </summary>
    public Dictionary<string, object>? JobState { get; set; }
}
