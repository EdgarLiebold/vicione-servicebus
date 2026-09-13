using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Requests execution capacity for a job from its distributed job-type coordinator.</summary>
public interface IAllocateJobSlot
{
    /// <summary>Gets the stable identity of the job type requesting capacity.</summary>
    Guid JobTypeId { get; }

    /// <summary>Gets the maximum duration for which an allocated slot may remain active.</summary>
    TimeSpan JobTimeout { get; }

    /// <summary>Gets the job that requires a slot.</summary>
    Guid JobId { get; }

    /// <summary>Gets the optional job metadata available to the distribution strategy.</summary>
    IReadOnlyDictionary<string, object>? JobProperties { get; }
}
