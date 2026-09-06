using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using ViciOne.ServiceBus.SignalR.Contracts;
using ViciOne.ServiceBus.SignalR.Utils;

namespace ViciOne.ServiceBus.SignalR.Consumers;

/// <summary>Consumes user messages.</summary>
/// <typeparam name="THub">The hub type.</typeparam>
public class UserConsumer<THub> :
    IConsumer<User<THub>>
    where THub : Hub
{
    readonly ViciOneServiceBusHubLifetimeManager<THub> _hubLifetimeManager;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="hubLifetimeManager">The hub lifetime manager.</param>
    public UserConsumer(ViciOneServiceBusHubLifetimeManager<THub> hubLifetimeManager)
    {
        _hubLifetimeManager = hubLifetimeManager;
    }

    /// <summary>Consumes the message provided by the context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ConsumeAsync(ConsumeContext<User<THub>> context)
    {
        return HandleAsync(context.Message.UserId, context.Message.Messages);
    }

    async Task HandleAsync(string userId, IReadOnlyDictionary<string, byte[]> messages)
    {
        var message = new Lazy<SerializedHubMessage>(messages.ToSerializedHubMessage);

        var userStore = _hubLifetimeManager.Users[userId];

        if (userStore == null || userStore.Count <= 0)
            return;

        var tasks = new List<Task>(userStore.Count);
        foreach (var connection in userStore)
            tasks.Add(connection.WriteAsync(message.Value).AsTask());

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
