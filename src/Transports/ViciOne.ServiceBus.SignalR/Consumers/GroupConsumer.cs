using Microsoft.AspNetCore.SignalR;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.SignalR.Contracts;
using ViciOne.ServiceBus.SignalR.Runtime;

namespace ViciOne.ServiceBus.SignalR.Consumers;

/// <summary>Delivers a group invocation to the group's eligible local members.</summary>
internal sealed class GroupConsumer<THub> :
    IConsumer<GroupMessage<THub>>
    where THub : Hub
{
    readonly ServiceBusHubLifetimeManager<THub> _lifetimeManager;

    /// <summary>Initializes the consumer with the node-local subscription index.</summary>
    public GroupConsumer(ServiceBusHubLifetimeManager<THub> lifetimeManager)
    {
        _lifetimeManager = lifetimeManager ?? throw new ArgumentNullException(nameof(lifetimeManager));
    }

    /// <summary>Writes the invocation to a snapshot of the local group membership.</summary>
    public Task ConsumeAsync(ConsumeContext<GroupMessage<THub>> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return DeliverAsync(context.Message);
    }

    async Task DeliverAsync(GroupMessage<THub> backplaneMessage)
    {
        HubConnectionContext[] connections = _lifetimeManager.Groups.GetConnections(backplaneMessage.GroupName);
        if (connections.Length == 0)
            return;

        SerializedHubMessage invocation = backplaneMessage.ProtocolPayloads.ToSerializedHubMessage();
        var excluded = new HashSet<string>(backplaneMessage.ExcludedConnectionIds, StringComparer.Ordinal);
        var writes = new List<Task>(connections.Length);
        foreach (HubConnectionContext connection in connections)
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
            LogContext.Warning?.Log(
                exception,
                "A SignalR group invocation could not be written to every local member of group {GroupName}.",
                backplaneMessage.GroupName);
        }
    }
}
