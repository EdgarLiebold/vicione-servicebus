using Microsoft.AspNetCore.SignalR;

namespace ViciOne.ServiceBus.SignalR.Runtime;

/// <summary>Owns the case-sensitive group memberships established for one local connection.</summary>
internal sealed class ConnectionGroupFeature
{
    readonly Lock _gate = new();
    readonly HashSet<string> _groups = new(StringComparer.Ordinal);
    bool _closed;

    /// <summary>Adds a group when the connection is not already a member.</summary>
    /// <returns><see langword="true" /> when the membership was added.</returns>
    public bool Add(string groupName)
    {
        ArgumentNullException.ThrowIfNull(groupName);

        lock (_gate)
            return !_closed && _groups.Add(groupName);
    }

    /// <summary>Removes a group when the connection is currently a member.</summary>
    /// <returns><see langword="true" /> when the membership was removed.</returns>
    public bool Remove(string groupName)
    {
        ArgumentNullException.ThrowIfNull(groupName);

        lock (_gate)
            return !_closed && _groups.Remove(groupName);
    }

    /// <summary>Adds feature and index membership under the same connection gate.</summary>
    public void Add(string groupName, HubConnectionContext connection, ConnectionSubscriptionIndex index)
    {
        ArgumentNullException.ThrowIfNull(groupName);
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(index);
        lock (_gate)
        {
            if (!_closed && _groups.Add(groupName))
                index.AddSubscription(groupName, connection);
        }
    }

    /// <summary>Removes feature and index membership under the same connection gate.</summary>
    public void Remove(string groupName, HubConnectionContext connection, ConnectionSubscriptionIndex index)
    {
        ArgumentNullException.ThrowIfNull(groupName);
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(index);
        lock (_gate)
        {
            if (!_closed && _groups.Remove(groupName))
                index.RemoveSubscription(groupName, connection);
        }
    }

    /// <summary>Closes this connection and removes every indexed membership atomically with group mutations.</summary>
    public void Close(HubConnectionContext connection, ConnectionSubscriptionIndex index)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(index);
        lock (_gate)
        {
            if (_closed)
                return;
            _closed = true;
            foreach (string groupName in _groups)
                index.RemoveSubscription(groupName, connection);
            _groups.Clear();
        }
    }

    /// <summary>Returns a stable snapshot for disconnect cleanup.</summary>
    public string[] Snapshot()
    {
        lock (_gate)
            return [.. _groups];
    }
}
