using Microsoft.AspNetCore.SignalR;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.SignalR.Contracts;
using ViciOne.ServiceBus.SignalR.Runtime;

namespace ViciOne.ServiceBus.SignalR.Consumers;

/// <summary>Completes a pending invocation on the node that originally requested a client result.</summary>
internal sealed class ClientResultConsumer<THub> :
    IConsumer<ClientResultMessage<THub>>
    where THub : Hub
{
    readonly ServiceBusHubLifetimeManager<THub> _lifetimeManager;

    /// <summary>Initializes the consumer with the node-local invocation tracker.</summary>
    public ClientResultConsumer(ServiceBusHubLifetimeManager<THub> lifetimeManager)
    {
        _lifetimeManager = lifetimeManager ?? throw new ArgumentNullException(nameof(lifetimeManager));
    }

    /// <summary>Ignores results for other nodes and parses a result addressed to this node.</summary>
    public Task ConsumeAsync(ConsumeContext<ClientResultMessage<THub>> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _lifetimeManager.ReceiveClientResult(context.Message);
        return Task.CompletedTask;
    }
}
