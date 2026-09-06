using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Defines the operations required by save job state.</summary>
public interface SaveJobState
{
    /// <summary>Gets the job id.</summary>
    Guid JobId { get; }

    /// <summary>Gets the attempt id.</summary>
    Guid AttemptId { get; }

    /// <summary>The state of the job, as a dictionary, or null to clear the state.</summary>
    Dictionary<string, object>? JobState { get; }
}
