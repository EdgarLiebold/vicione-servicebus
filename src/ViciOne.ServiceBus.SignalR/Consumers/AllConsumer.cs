using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using ViciOne.ServiceBus.SignalR.Contracts;
using ViciOne.ServiceBus.SignalR.Utils;

namespace ViciOne.ServiceBus.SignalR.Consumers;

/// <summary>
/// Provides an all consumer implementation.
/// </summary>
/// <typeparam name="THub">The t hub type.</typeparam>
public class AllConsumer<THub> :
    IConsumer<All<THub>>
    where THub : Hub
{
    readonly ViciOneServiceBusHubLifetimeManager<THub> _hubLifetimeManager;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hubLifetimeManager">The hub lifetime manager value.</param>
    public AllConsumer(ViciOneServiceBusHubLifetimeManager<THub> hubLifetimeManager)
    {
        _hubLifetimeManager = hubLifetimeManager;
    }

    /// <summary>
    /// Consumes the message provided by the context.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
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
