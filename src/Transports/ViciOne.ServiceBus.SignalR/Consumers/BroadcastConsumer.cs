using Microsoft.AspNetCore.SignalR;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.SignalR.Contracts;
using ViciOne.ServiceBus.SignalR.Runtime;

namespace ViciOne.ServiceBus.SignalR.Consumers;

/// <summary>Delivers a broadcast backplane message to every eligible local connection.</summary>
internal sealed class BroadcastConsumer<THub> :
    IConsumer<BroadcastMessage<THub>>
    where THub : Hub
{
    readonly ServiceBusHubLifetimeManager<THub> _lifetimeManager;

    /// <summary>Initializes the consumer with the node-local connection owner.</summary>
    public BroadcastConsumer(ServiceBusHubLifetimeManager<THub> lifetimeManager)
    {
        _lifetimeManager = lifetimeManager ?? throw new ArgumentNullException(nameof(lifetimeManager));
    }

    /// <summary>Writes the invocation to all local connections not named in the exclusion set.</summary>
    public Task ConsumeAsync(ConsumeContext<BroadcastMessage<THub>> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return DeliverAsync(context.Message);
    }

    async Task DeliverAsync(BroadcastMessage<THub> backplaneMessage)
    {
        if (_lifetimeManager.Connections.Count == 0)
            return;

        SerializedHubMessage invocation = backplaneMessage.ProtocolPayloads.ToSerializedHubMessage();
        var excluded = new HashSet<string>(backplaneMessage.ExcludedConnectionIds, StringComparer.Ordinal);
        var writes = new List<Task>(_lifetimeManager.Connections.Count);
        foreach (HubConnectionContext connection in _lifetimeManager.Connections)
        {
            if (!excluded.Contains(connection.ConnectionId))
                writes.Add(connection.WriteAsync(invocation).AsTask());
        }

        try
        {
            await Task.WhenAll(writes).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            LogContext.Warning?.Log(exception, "A SignalR broadcast could not be written to every local connection.");
        }
    }
}
