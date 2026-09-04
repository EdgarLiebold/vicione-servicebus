using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using ViciOne.ServiceBus.SignalR.Contracts;
using ViciOne.ServiceBus.SignalR.Utils;

namespace ViciOne.ServiceBus.SignalR.Consumers;

public class ConnectionConsumer<THub> :
    IConsumer<Connection<THub>>
    where THub : Hub
{
    readonly ViciOneServiceBusHubLifetimeManager<THub> _hubLifetimeManager;

    public ConnectionConsumer(ViciOneServiceBusHubLifetimeManager<THub> hubLifetimeManager)
    {
        _hubLifetimeManager = hubLifetimeManager;
    }

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
