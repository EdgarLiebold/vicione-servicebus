using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using ViciOne.ServiceBus.SignalR.Contracts;

namespace ViciOne.ServiceBus.SignalR.Consumers;

/// <summary>Consumes group management messages.</summary>
/// <typeparam name="THub">The hub type.</typeparam>
public class GroupManagementConsumer<THub> :
    IConsumer<GroupManagement<THub>>
    where THub : Hub
{
    readonly ViciOneServiceBusHubLifetimeManager<THub> _hubLifetimeManager;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="hubLifetimeManager">The hub lifetime manager.</param>
    public GroupManagementConsumer(ViciOneServiceBusHubLifetimeManager<THub> hubLifetimeManager)
    {
        _hubLifetimeManager = hubLifetimeManager;
    }

    /// <summary>Consumes the message provided by the context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ConsumeAsync(ConsumeContext<GroupManagement<THub>> context)
    {
        var connection = _hubLifetimeManager.Connections[context.Message.ConnectionId];

        if (connection == null)
            return Task.CompletedTask; // Connection doesn't exist on this server, no need to send Ack back

        if (context.Message.Action == GroupAction.Remove)
            _hubLifetimeManager.RemoveGroupCore(connection, context.Message.GroupName);
        else if (context.Message.Action == GroupAction.Add)
            _hubLifetimeManager.AddGroupCore(connection, context.Message.GroupName);

        return context.Advanced().RespondAsync<Ack<THub>>(new { _hubLifetimeManager.ServerName });
    }
}
