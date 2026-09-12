using Microsoft.AspNetCore.SignalR;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.SignalR.Contracts;
using ViciOne.ServiceBus.SignalR.Runtime;

namespace ViciOne.ServiceBus.SignalR.Consumers;

/// <summary>Applies an acknowledged group command on the node that owns its connection.</summary>
internal sealed class GroupCommandConsumer<THub> :
    IConsumer<GroupCommand<THub>>
    where THub : Hub
{
    readonly ServiceBusHubLifetimeManager<THub> _lifetimeManager;

    /// <summary>Initializes the consumer with the node-local connection owner.</summary>
    public GroupCommandConsumer(ServiceBusHubLifetimeManager<THub> lifetimeManager)
    {
        _lifetimeManager = lifetimeManager ?? throw new ArgumentNullException(nameof(lifetimeManager));
    }

    /// <summary>Responds only after the owning node has applied a supported membership action.</summary>
    public Task ConsumeAsync(ConsumeContext<GroupCommand<THub>> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        HubConnectionContext? connection = _lifetimeManager.Connections[context.Message.ConnectionId];

        if (connection is null)
            return Task.CompletedTask;

        switch (context.Message.Action)
        {
            case GroupCommandAction.Add:
                _lifetimeManager.AddGroup(connection, context.Message.GroupName);
                break;
            case GroupCommandAction.Remove:
                _lifetimeManager.RemoveGroup(connection, context.Message.GroupName);
                break;
            default:
                throw new InvalidDataException($"Unsupported SignalR group command action '{context.Message.Action}'.");
        }

        return context.Advanced().RespondAsync(
            new GroupCommandAcknowledgement<THub>(_lifetimeManager.NodeId));
    }
}
