// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Contracts.JobService;

using System;
using System.Collections.Generic;


public interface SaveJobState
{
    Guid JobId { get; }

    Guid AttemptId { get; }

    /// <summary>
    /// The state of the job, as a dictionary, or null to clear the state
    /// </summary>
    Dictionary<string, object>? JobState { get; }
}
