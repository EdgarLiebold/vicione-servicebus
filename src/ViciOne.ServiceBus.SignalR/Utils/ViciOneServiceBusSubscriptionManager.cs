using System;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

namespace ViciOne.ServiceBus.SignalR.Utils;

/// <summary>Manages vici one service bus subscription.</summary>
public class ViciOneServiceBusSubscriptionManager
{
    readonly ConcurrentDictionary<string, HubConnectionStore> _subscriptions = new ConcurrentDictionary<string, HubConnectionStore>(StringComparer.Ordinal);

    /// <summary>Gets or sets the value at the specified index.</summary>
    /// <param name="identifier">The identifier.</param>
    public HubConnectionStore? this[string identifier]
    {
        get
        {
            _subscriptions.TryGetValue(identifier, out var connectionStore);
            return connectionStore;
        }
    }

    /// <summary>Gets the count.</summary>
    public int Count => _subscriptions.Count;

    /// <summary>Adds subscription to the configuration.</summary>
    /// <param name="id">The id.</param>
    /// <param name="connection">The connection.</param>
    public void AddSubscription(string id, HubConnectionContext connection)
    {
        var subscription = _subscriptions.GetOrAdd(id, _ => new HubConnectionStore());

        subscription.Add(connection);
    }

    /// <summary>Removes subscription.</summary>
    /// <param name="id">The id.</param>
    /// <param name="connection">The connection.</param>
    public void RemoveSubscription(string id, HubConnectionContext connection)
    {
        if (!_subscriptions.TryGetValue(id, out var subscription))
            return;

        subscription.Remove(connection);
    }
}
