using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>
/// Defines the contract for complete job.
/// </summary>
[ConfigureConsumeTopology(false)]
public interface CompleteJob
{
    /// <summary>
    /// Gets the job id value.
    /// </summary>
    Guid JobId { get; }

    /// <summary>
    /// Gets the timestamp value.
    /// </summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets the duration value.
    /// </summary>
    TimeSpan Duration { get; }

    /// <summary>
    /// The job, as an object dictionary
    /// </summary>
    Dictionary<string, object> Job { get; }

    /// <summary>
    /// The JobTypeId, to ensure the proper job type is started
    /// </summary>
    Guid JobTypeId { get; }

    /// <summary>
    /// Gets the job properties value.
    /// </summary>
    Dictionary<string, object>? JobProperties { get; }

    /// <summary>
    /// Gets the instance properties value.
    /// </summary>
    Dictionary<string, object>? InstanceProperties { get; }

    /// <summary>
    /// Gets the job type properties value.
    /// </summary>
    Dictionary<string, object>? JobTypeProperties { get; }
}
