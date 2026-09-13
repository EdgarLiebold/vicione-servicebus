using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Describes one active allocation exposed to a job distribution strategy.</summary>
public sealed class JobAllocationInfo
{
    internal JobAllocationInfo(JobAllocationState allocation)
    {
        ArgumentNullException.ThrowIfNull(allocation);

        JobId = allocation.JobId;
        ExpiresAt = allocation.ExpiresAt;
        InstanceAddress = allocation.InstanceAddress;
        JobProperties = new ReadOnlyDictionary<string, object>(JobPropertySnapshot.Create(allocation.Properties));
    }

    /// <summary>Gets the identifier of the allocated job.</summary>
    public Guid JobId { get; }

    /// <summary>Gets the instant at which the uncompleted allocation may be reclaimed.</summary>
    public DateTimeOffset ExpiresAt { get; }

    /// <summary>Gets the endpoint address of the assigned service instance.</summary>
    public Uri InstanceAddress { get; }

    /// <summary>Gets the job metadata captured when the allocation was created.</summary>
    public IReadOnlyDictionary<string, object> JobProperties { get; }
}
