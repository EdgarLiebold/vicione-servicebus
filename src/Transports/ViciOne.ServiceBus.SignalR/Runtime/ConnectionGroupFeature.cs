namespace ViciOne.ServiceBus.SignalR.Runtime;

/// <summary>Owns the case-sensitive group memberships established for one local connection.</summary>
internal sealed class ConnectionGroupFeature
{
    readonly Lock _gate = new();
    readonly HashSet<string> _groups = new(StringComparer.Ordinal);

    /// <summary>Adds a group when the connection is not already a member.</summary>
    /// <returns><see langword="true" /> when the membership was added.</returns>
    public bool Add(string groupName)
    {
        ArgumentNullException.ThrowIfNull(groupName);

        lock (_gate)
            return _groups.Add(groupName);
    }

    /// <summary>Removes a group when the connection is currently a member.</summary>
    /// <returns><see langword="true" /> when the membership was removed.</returns>
    public bool Remove(string groupName)
    {
        ArgumentNullException.ThrowIfNull(groupName);

        lock (_gate)
            return _groups.Remove(groupName);
    }

    /// <summary>Returns a stable snapshot for disconnect cleanup.</summary>
    public string[] Snapshot()
    {
        lock (_gate)
            return [.. _groups];
    }
}
