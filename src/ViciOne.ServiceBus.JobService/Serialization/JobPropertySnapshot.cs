using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Creates isolated case-insensitive snapshots of job metadata and serialized values.</summary>
internal static class JobPropertySnapshot
{
    /// <summary>Copies values using deterministic last-write-wins handling for case-equivalent keys.</summary>
    /// <param name="properties">The values to copy, or <see langword="null" /> for an empty snapshot.</param>
    /// <returns>An independent case-insensitive dictionary.</returns>
    public static Dictionary<string, object> Create(IEnumerable<KeyValuePair<string, object>>? properties)
    {
        var snapshot = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        if (properties is null)
            return snapshot;

        foreach (KeyValuePair<string, object> property in properties)
            snapshot[property.Key] = property.Value;

        return snapshot;
    }
}
