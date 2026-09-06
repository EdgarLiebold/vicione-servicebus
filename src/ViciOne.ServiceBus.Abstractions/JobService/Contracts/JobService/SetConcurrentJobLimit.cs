using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>When the bus is started, the current job limit for a job type is published along with the instance address.</summary>
public interface SetConcurrentJobLimit
{
    /// <summary>Gets the job type id.</summary>
    Guid JobTypeId { get; }

    /// <summary>Gets the instance address.</summary>
    Uri InstanceAddress { get; }

    /// <summary>Gets the concurrent job limit.</summary>
    int ConcurrentJobLimit { get; }

    /// <summary>Gets the kind.</summary>
    ConcurrentLimitKind Kind { get; }

    /// <summary>How long a overridden limit should be in effect.</summary>
    TimeSpan? Duration { get; }

    /// <summary>If present, the job type name.</summary>
    string? JobTypeName { get; }

    /// <summary>Allows properties to be submitted by the job service instance that can be used by the job distribution strategy.</summary>
    Dictionary<string, object>? JobTypeProperties { get; }

    /// <summary>Allows properties to be submitted by the job service instance that can be used by the job distribution strategy.</summary>
    Dictionary<string, object>? InstanceProperties { get; }

    /// <summary>If configured, specifies a global limit across all job consumer instances.</summary>
    int? GlobalConcurrentJobLimit { get; }
}
