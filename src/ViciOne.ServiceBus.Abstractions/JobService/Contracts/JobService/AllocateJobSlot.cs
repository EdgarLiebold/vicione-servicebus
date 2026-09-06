using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Defines the operations required by allocate job slot.</summary>
public interface AllocateJobSlot
{
    /// <summary>Gets the job type id.</summary>
    Guid JobTypeId { get; }

    /// <summary>Gets the job timeout.</summary>
    TimeSpan JobTimeout { get; }

    /// <summary>Gets the job id.</summary>
    Guid JobId { get; }

    /// <summary>The job properties.</summary>
    Dictionary<string, object>? JobProperties { get; }
}
