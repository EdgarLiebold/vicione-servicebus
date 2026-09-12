using Microsoft.AspNetCore.SignalR;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.SignalR.Contracts;
using ViciOne.ServiceBus.SignalR.Runtime;

namespace ViciOne.ServiceBus.SignalR.Consumers;

/// <summary>Delivers a user invocation to every local connection for that user.</summary>
internal sealed class UserConsumer<THub> :
    IConsumer<UserMessage<THub>>
    where THub : Hub
{
    readonly ServiceBusHubLifetimeManager<THub> _lifetimeManager;

    /// <summary>Initializes the consumer with the node-local subscription index.</summary>
    public UserConsumer(ServiceBusHubLifetimeManager<THub> lifetimeManager)
    {
        _lifetimeManager = lifetimeManager ?? throw new ArgumentNullException(nameof(lifetimeManager));
    }

    /// <summary>Writes the invocation to a snapshot of the local user's connections.</summary>
    public Task ConsumeAsync(ConsumeContext<UserMessage<THub>> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return DeliverAsync(context.Message);
    }

    async Task DeliverAsync(UserMessage<THub> backplaneMessage)
    {
        HubConnectionContext[] connections = _lifetimeManager.Users.GetConnections(backplaneMessage.UserId);
        if (connections.Length == 0)
            return;

        SerializedHubMessage invocation = backplaneMessage.ProtocolPayloads.ToSerializedHubMessage();
        var writes = new List<Task>(connections.Length);
        foreach (HubConnectionContext connection in connections)
            writes.Add(connection.WriteAsync(invocation).AsTask());

        try
        {
            await Task.WhenAll(writes).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            LogContext.Warning?.Log(
                exception,
                "A SignalR user invocation could not be written to every local connection for user {UserId}.",
                backplaneMessage.UserId);
        }
    }
}
