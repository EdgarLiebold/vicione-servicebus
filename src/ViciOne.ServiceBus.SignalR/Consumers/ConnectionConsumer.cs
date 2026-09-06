using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using ViciOne.ServiceBus.SignalR.Contracts;
using ViciOne.ServiceBus.SignalR.Utils;

namespace ViciOne.ServiceBus.SignalR.Consumers;

/// <summary>Consumes connection messages.</summary>
/// <typeparam name="THub">The hub type.</typeparam>
public class ConnectionConsumer<THub> :
    IConsumer<Connection<THub>>
    where THub : Hub
{
    readonly ViciOneServiceBusHubLifetimeManager<THub> _hubLifetimeManager;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="hubLifetimeManager">The hub lifetime manager.</param>
    public ConnectionConsumer(ViciOneServiceBusHubLifetimeManager<THub> hubLifetimeManager)
    {
        _hubLifetimeManager = hubLifetimeManager;
    }

    /// <summary>Consumes the message provided by the context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ConsumeAsync(ConsumeContext<Connection<THub>> context)
    {
        return HandleAsync(context.Message.ConnectionId, context.Message.Messages);
    }

    async Task HandleAsync(string connectionId, IReadOnlyDictionary<string, byte[]> messages)
    {
        var message = new Lazy<SerializedHubMessage>(messages.ToSerializedHubMessage);

        var connection = _hubLifetimeManager.Connections[connectionId];
        if (connection == null)
            return; // Connection doesn't exist on server, skipping

        try
        {
            await connection.WriteAsync(message.Value).AsTask();
        }
        catch (Exception e)
        {
            LogContext.Warning?.Log(e, "Failed to write message");
        }
    }
}
