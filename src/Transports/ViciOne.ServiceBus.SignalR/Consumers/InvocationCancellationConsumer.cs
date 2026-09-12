using Microsoft.AspNetCore.SignalR;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.SignalR.Contracts;
using ViciOne.ServiceBus.SignalR.Runtime;

namespace ViciOne.ServiceBus.SignalR.Consumers;

/// <summary>Cancels a pending client invocation on the node that owns its connection.</summary>
internal sealed class InvocationCancellationConsumer<THub> :
    IConsumer<InvocationCancellationMessage<THub>>
    where THub : Hub
{
    readonly ServiceBusHubLifetimeManager<THub> _lifetimeManager;

    /// <summary>Initializes the consumer with the node-local invocation tracker.</summary>
    public InvocationCancellationConsumer(ServiceBusHubLifetimeManager<THub> lifetimeManager)
    {
        _lifetimeManager = lifetimeManager ?? throw new ArgumentNullException(nameof(lifetimeManager));
    }

    /// <summary>Forwards cancellation only when this node owns the exact pending invocation.</summary>
    public Task ConsumeAsync(ConsumeContext<InvocationCancellationMessage<THub>> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return _lifetimeManager.CancelRemoteInvocationAsync(
            context.Message.ConnectionId,
            context.Message.InvocationId,
            context.CancellationToken);
    }
}
