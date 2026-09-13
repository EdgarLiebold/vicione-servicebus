using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Describes one live service instance exposed to a job distribution strategy.</summary>
public sealed class JobServiceInstanceInfo
{
    internal JobServiceInstanceInfo(JobServiceInstanceState instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        LastHeartbeatAt = instance.LastHeartbeatAt;
        LastAllocationAt = instance.LastAllocationAt;
        InstanceProperties = new ReadOnlyDictionary<string, object>(JobPropertySnapshot.Create(instance.Properties));
    }

    /// <summary>Gets the instant of the latest availability heartbeat.</summary>
    public DateTimeOffset? LastHeartbeatAt { get; }

    /// <summary>Gets the instant at which this instance most recently received an allocation.</summary>
    public DateTimeOffset? LastAllocationAt { get; }

    /// <summary>Gets instance metadata available to distribution strategies.</summary>
    public IReadOnlyDictionary<string, object> InstanceProperties { get; }
}
