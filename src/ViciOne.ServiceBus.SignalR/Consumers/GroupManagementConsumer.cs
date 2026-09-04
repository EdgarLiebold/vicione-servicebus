using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using ViciOne.ServiceBus.SignalR.Contracts;

namespace ViciOne.ServiceBus.SignalR.Consumers;

/// <summary>
/// Provides a group management consumer implementation.
/// </summary>
/// <typeparam name="THub">The t hub type.</typeparam>
public class GroupManagementConsumer<THub> :
    IConsumer<GroupManagement<THub>>
    where THub : Hub
{
    readonly ViciOneServiceBusHubLifetimeManager<THub> _hubLifetimeManager;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hubLifetimeManager">The hub lifetime manager value.</param>
    public GroupManagementConsumer(ViciOneServiceBusHubLifetimeManager<THub> hubLifetimeManager)
    {
        _hubLifetimeManager = hubLifetimeManager;
    }

    /// <summary>
    /// Consumes the message provided by the context.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task ConsumeAsync(ConsumeContext<GroupManagement<THub>> context)
    {
        var connection = _hubLifetimeManager.Connections[context.Message.ConnectionId];

        if (connection == null)
            return Task.CompletedTask; // Connection doesn't exist on this server, no need to send Ack back

        if (context.Message.Action == GroupAction.Remove)
            _hubLifetimeManager.RemoveGroupAsyncCore(connection, context.Message.GroupName);
        else if (context.Message.Action == GroupAction.Add)
            _hubLifetimeManager.AddGroupAsyncCore(connection, context.Message.GroupName);

        return context.Advanced().RespondAsync<Ack<THub>>(new { _hubLifetimeManager.ServerName });
    }
}
