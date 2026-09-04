using System;
using System.Collections.Generic;

#nullable enable
namespace ViciOne.ServiceBus.JobService;
/// <summary>
/// Active Jobs are allocated a concurrency slot, and are valid until the deadline is reached, after
/// which they may be automatically released.
/// </summary>
public class ActiveJob :
    IEquatable<ActiveJob>
{
    /// <summary>
    /// Gets or sets the job id value.
    /// </summary>
    public Guid JobId { get; set; }

    /// <summary>
    /// Calculated from the JobTimeout based on the time the job slot was requested, not currently used
    /// </summary>
    public DateTimeOffset Deadline { get; set; }

    /// <summary>
    /// The instance assigned to the job
    /// </summary>
    public Uri InstanceAddress { get; set; } = null!;

    /// <summary>
    /// Gets or sets the properties value.
    /// </summary>
    public Dictionary<string, object>? Properties { get; set; }

    /// <summary>
    /// Determines whether this instance equals the supplied value.
    /// </summary>
    /// <param name="other">The other value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Equals(ActiveJob? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return JobId.Equals(other.JobId);
    }

    /// <summary>
    /// Determines whether this instance equals the supplied value.
    /// </summary>
    /// <param name="obj">The obj value.</param>
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

    /// <summary>
    /// Gets hash code.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override int GetHashCode()
    {
        // ReSharper disable once NonReadonlyMemberInGetHashCode
        return JobId.GetHashCode();
    }
}
