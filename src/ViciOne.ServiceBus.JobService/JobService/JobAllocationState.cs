using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Persists a job allocation and the service instance that owns its concurrency slot.</summary>
public sealed class JobAllocationState :
    IEquatable<JobAllocationState>
{
    /// <summary>Gets or sets the identifier of the job that owns the slot.</summary>
    public Guid JobId { get; set; }

    /// <summary>Gets or sets the instant at which an uncompleted allocation may be reclaimed.</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>Gets or sets the endpoint address of the assigned service instance.</summary>
    public Uri InstanceAddress { get; set; } = null!;

    /// <summary>Gets or sets the job metadata captured when the allocation was created.</summary>
    public Dictionary<string, object>? Properties { get; set; }

    /// <summary>Compares allocations by job identifier.</summary>
    /// <param name="other">The allocation to compare.</param>
    /// <returns><see langword="true" /> when both allocations identify the same job; otherwise, <see langword="false" />.</returns>
    public bool Equals(JobAllocationState? other)
    {
        return other is not null && (ReferenceEquals(this, other) || JobId == other.JobId);
    }

    /// <summary>Compares an object with this allocation by job identifier.</summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns><see langword="true" /> when <paramref name="obj" /> identifies the same job; otherwise, <see langword="false" />.</returns>
    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is JobAllocationState other && Equals(other);
    }

    /// <summary>Returns the hash code of the job identifier.</summary>
    /// <returns>The allocation hash code.</returns>
    public override int GetHashCode()
    {
        return JobId.GetHashCode();
    }
}
