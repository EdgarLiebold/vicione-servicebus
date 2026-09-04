using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using ViciOne.ServiceBus.SignalR.Contracts;
using ViciOne.ServiceBus.SignalR.Utils;

namespace ViciOne.ServiceBus.SignalR.Consumers;

public class AllConsumer<THub> :
    IConsumer<All<THub>>
    where THub : Hub
{
    readonly ViciOneServiceBusHubLifetimeManager<THub> _hubLifetimeManager;

    public AllConsumer(ViciOneServiceBusHubLifetimeManager<THub> hubLifetimeManager)
    {
        _hubLifetimeManager = hubLifetimeManager;
    }

    public Task ConsumeAsync(ConsumeContext<All<THub>> context)
    {
        return HandleAsync(context.Message.ExcludedConnectionIds, context.Message.Messages);
    }

    async Task HandleAsync(string[] excludedConnectionIds, IReadOnlyDictionary<string, byte[]> messages)
    {
        var message = new Lazy<SerializedHubMessage>(messages.ToSerializedHubMessage);

        var tasks = new List<Task>(_hubLifetimeManager.Connections.Count);

        foreach (var connection in _hubLifetimeManager.Connections)
        {
            if (excludedConnectionIds == null || !excludedConnectionIds.Contains(connection.ConnectionId, StringComparer.Ordinal))
                tasks.Add(connection.WriteAsync(message.Value).AsTask());
        }

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception e)
        {
            LogContext.Warning?.Log(e, "Failed to write message");
        }
    }
}
