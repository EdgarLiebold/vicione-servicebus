using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Announces or overrides execution capacity for a job type and service instance.</summary>
public interface SetConcurrentJobLimit
{
    /// <summary>Gets the stable identity of the job type and endpoint.</summary>
    Guid JobTypeId { get; }

    /// <summary>Gets the address of the service instance announcing capacity.</summary>
    Uri InstanceAddress { get; }

    /// <summary>Gets the maximum concurrent jobs accepted by the instance.</summary>
    int ConcurrentJobLimit { get; }

    /// <summary>Gets how this update changes coordinator state.</summary>
    JobConcurrencyUpdateKind UpdateKind { get; }

    /// <summary>Gets how long an override remains effective.</summary>
    TimeSpan? Duration { get; }

    /// <summary>Gets the diagnostic job-type name.</summary>
    string? JobTypeName { get; }

    /// <summary>Gets metadata shared by all instances of the job type.</summary>
    IReadOnlyDictionary<string, object>? JobTypeProperties { get; }

    /// <summary>Gets metadata of the announcing service instance.</summary>
    IReadOnlyDictionary<string, object>? InstanceProperties { get; }

    /// <summary>Gets the optional maximum concurrency across all service instances of the job type.</summary>
    int? GlobalConcurrentJobLimit { get; }
}
