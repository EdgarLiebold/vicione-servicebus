using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Represents a job that owns a concurrency slot and identifies the service instance assigned to it.</summary>
public sealed class ActiveJob :
    IEquatable<ActiveJob>
{
    /// <summary>Gets or sets the identifier of the job that owns the slot.</summary>
    public Guid JobId { get; set; }

    /// <summary>Gets or sets the instant at which an uncompleted allocation may be reclaimed.</summary>
    public DateTimeOffset Deadline { get; set; }

    /// <summary>Gets or sets the endpoint address of the assigned service instance.</summary>
    public Uri InstanceAddress { get; set; } = null!;

    /// <summary>Gets or sets the job metadata available to distribution strategies.</summary>
    public Dictionary<string, object>? JobProperties { get; set; }

    /// <summary>Compares allocations by job identifier.</summary>
    /// <param name="other">The allocation to compare.</param>
    /// <returns><see langword="true" /> when both allocations identify the same job; otherwise, <see langword="false" />.</returns>
    public bool Equals(ActiveJob? other)
    {
        return other is not null && (ReferenceEquals(this, other) || JobId == other.JobId);
    }

    /// <summary>Compares an object with this allocation by job identifier.</summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns><see langword="true" /> when <paramref name="obj" /> identifies the same job; otherwise, <see langword="false" />.</returns>
    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is ActiveJob other && Equals(other);
    }

    /// <summary>Returns the hash code of the job identifier.</summary>
    /// <returns>The allocation hash code.</returns>
    public override int GetHashCode()
    {
        return JobId.GetHashCode();
    }
}
