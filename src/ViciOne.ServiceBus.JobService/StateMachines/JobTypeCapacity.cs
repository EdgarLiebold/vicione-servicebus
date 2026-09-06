using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Maintains the persisted allocation and service-instance invariants of a job type.</summary>
internal static class JobTypeCapacity
{
    /// <summary>Removes allocations that can no longer identify a live, valid execution slot.</summary>
    /// <param name="saga">The job-type state to reconcile.</param>
    /// <param name="timestamp">The coordinator time used for lease and deadline comparisons.</param>
    /// <param name="heartbeatTimeout">The maximum interval between instance heartbeats.</param>
    public static void RemoveExpiredAllocations(JobTypeSaga saga, DateTimeOffset timestamp, TimeSpan heartbeatTimeout)
    {
        ArgumentNullException.ThrowIfNull(saga);
        if (heartbeatTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(heartbeatTimeout), heartbeatTimeout, "The heartbeat timeout must be positive.");

        HashSet<Uri> expiredInstances = saga.Instances
            .Where(pair => pair.Value.Updated is not DateTimeOffset updated || timestamp - updated > heartbeatTimeout)
            .Select(static pair => pair.Key)
            .ToHashSet();

        foreach (Uri instanceAddress in expiredInstances)
            saga.Instances.Remove(instanceAddress);

        saga.ActiveJobs.RemoveAll(allocation =>
            allocation.Deadline <= timestamp || !saga.Instances.ContainsKey(allocation.InstanceAddress));
        saga.ActiveJobCount = saga.ActiveJobs.Count;
    }

    /// <summary>Creates a case-insensitive metadata snapshot and treats missing metadata as empty.</summary>
    /// <param name="properties">The metadata supplied by a configuration update.</param>
    /// <returns>An independent metadata dictionary.</returns>
    public static Dictionary<string, object> CopyProperties(IReadOnlyDictionary<string, object>? properties)
    {
        return properties?.ToDictionary(
            static pair => pair.Key,
            static pair => pair.Value,
            StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    }
}
