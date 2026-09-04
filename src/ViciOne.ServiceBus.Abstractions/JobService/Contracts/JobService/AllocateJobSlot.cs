using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>
/// Defines the contract for allocate job slot.
/// </summary>
public interface AllocateJobSlot
{
    /// <summary>
    /// Gets the job type id value.
    /// </summary>
    Guid JobTypeId { get; }

    /// <summary>
    /// Gets the job timeout value.
    /// </summary>
    TimeSpan JobTimeout { get; }

    /// <summary>
    /// Gets the job id value.
    /// </summary>
    Guid JobId { get; }

    /// <summary>
    /// The job properties
    /// </summary>
    Dictionary<string, object>? JobProperties { get; }
}
