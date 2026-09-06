using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.JobService;
/// <summary>Represents a job that owns a concurrency slot and identifies the service instance assigned to it.</summary>
public class ActiveJob :
    IEquatable<ActiveJob>
{
    /// <summary>Gets or sets the job id.</summary>
    public Guid JobId { get; set; }

    /// <summary>Gets or sets the expected expiration time calculated from the allocation time and job timeout.</summary>
    public DateTimeOffset Deadline { get; set; }

    /// <summary>The instance assigned to the job.</summary>
    public Uri InstanceAddress { get; set; } = null!;

    /// <summary>Gets or sets the properties.</summary>
    public Dictionary<string, object>? Properties { get; set; }

    /// <summary>Determines whether this instance equals the supplied value.</summary>
    /// <param name="other">The other.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Equals(ActiveJob? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return JobId.Equals(other.JobId);
    }

    /// <summary>Determines whether this instance equals the supplied value.</summary>
    /// <param name="obj">The obj.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        if (obj.GetType() != GetType())
            return false;
        return Equals((ActiveJob)obj);
    }

    /// <summary>Gets hash code.</summary>
    /// <returns>The hash code for this instance.</returns>
    public override int GetHashCode()
    {
        return JobId.GetHashCode();
    }
}
