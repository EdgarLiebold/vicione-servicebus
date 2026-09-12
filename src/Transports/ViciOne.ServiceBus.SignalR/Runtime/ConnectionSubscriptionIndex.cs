using Microsoft.AspNetCore.SignalR;

namespace ViciOne.ServiceBus.SignalR.Runtime;

/// <summary>Indexes local SignalR connections by a case-sensitive group or user identifier.</summary>
internal sealed class ConnectionSubscriptionIndex
{
    readonly Lock _gate = new();
    readonly Dictionary<string, HubConnectionStore> _subscriptions = new(StringComparer.Ordinal);

    /// <summary>Gets the number of identifiers that currently have at least one connection.</summary>
    public int Count
    {
        get
        {
            lock (_gate)
                return _subscriptions.Count;
        }
    }

    /// <summary>Adds a connection under the supplied identifier without creating duplicate membership.</summary>
    public void AddSubscription(string id, HubConnectionContext connection)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(connection);

        lock (_gate)
        {
            if (!_subscriptions.TryGetValue(id, out HubConnectionStore? subscription))
            {
                subscription = new HubConnectionStore();
                _subscriptions.Add(id, subscription);
            }

            subscription.Add(connection);
        }
    }

    /// <summary>Removes a connection and drops the identifier when no subscriptions remain.</summary>
    public void RemoveSubscription(string id, HubConnectionContext connection)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(connection);

        lock (_gate)
        {
            if (!_subscriptions.TryGetValue(id, out HubConnectionStore? subscription))
                return;

            subscription.Remove(connection);

            if (subscription.Count == 0)
                _subscriptions.Remove(id);
        }
    }

    /// <summary>Returns a stable snapshot of the connections subscribed under an identifier.</summary>
    public HubConnectionContext[] GetConnections(string id)
    {
        ArgumentNullException.ThrowIfNull(id);

        lock (_gate)
        {
            return _subscriptions.TryGetValue(id, out HubConnectionStore? subscription)
                ? [.. subscription]
                : [];
        }
    }
}
