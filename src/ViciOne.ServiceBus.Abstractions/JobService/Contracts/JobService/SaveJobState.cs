using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Contracts.JobService;

public interface SaveJobState
{
    Guid JobId { get; }

    Guid AttemptId { get; }

    /// <summary>
    /// The state of the job, as a dictionary, or null to clear the state
    /// </summary>
    Dictionary<string, object>? JobState { get; }
}
