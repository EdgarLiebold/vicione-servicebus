using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using ViciOne.ServiceBus.SignalR.Contracts;
using ViciOne.ServiceBus.SignalR.Utils;

namespace ViciOne.ServiceBus.SignalR.Consumers;

/// <summary>Consumes group messages.</summary>
/// <typeparam name="THub">The hub type.</typeparam>
public class GroupConsumer<THub> :
    IConsumer<Group<THub>>
    where THub : Hub
{
    readonly ViciOneServiceBusHubLifetimeManager<THub> _hubLifetimeManager;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="hubLifetimeManager">The hub lifetime manager.</param>
    public GroupConsumer(ViciOneServiceBusHubLifetimeManager<THub> hubLifetimeManager)
    {
        _hubLifetimeManager = hubLifetimeManager;
    }

    /// <summary>Consumes the message provided by the context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ConsumeAsync(ConsumeContext<Group<THub>> context)
    {
        return HandleAsync(context.Message.GroupName, context.Message.ExcludedConnectionIds, context.Message.Messages);
    }

    async Task HandleAsync(string groupName, string[] excludedConnectionIds, IReadOnlyDictionary<string, byte[]> messages)
    {
        var message = new Lazy<SerializedHubMessage>(messages.ToSerializedHubMessage);

        var groupStore = _hubLifetimeManager.Groups[groupName];

        if (groupStore == null || groupStore.Count <= 0)
            return;

        var tasks = new List<Task>();
        foreach (var connection in groupStore)
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
