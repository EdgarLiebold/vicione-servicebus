using Microsoft.AspNetCore.SignalR;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.SignalR.Contracts;
using ViciOne.ServiceBus.SignalR.Runtime;

namespace ViciOne.ServiceBus.SignalR.Consumers;

/// <summary>Delivers an invocation only on the node that owns its target connection.</summary>
internal sealed class ConnectionConsumer<THub> :
    IConsumer<ConnectionMessage<THub>>
    where THub : Hub
{
    readonly ServiceBusHubLifetimeManager<THub> _lifetimeManager;

    /// <summary>Initializes the consumer with the node-local connection owner.</summary>
    public ConnectionConsumer(ServiceBusHubLifetimeManager<THub> lifetimeManager)
    {
        _lifetimeManager = lifetimeManager ?? throw new ArgumentNullException(nameof(lifetimeManager));
    }

    /// <summary>Ignores non-owned connections and suppresses concurrent duplicate result invocations.</summary>
    public Task ConsumeAsync(ConsumeContext<ConnectionMessage<THub>> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return DeliverAsync(context.Message);
    }

    async Task DeliverAsync(ConnectionMessage<THub> backplaneMessage)
    {
        HubConnectionContext? connection = _lifetimeManager.Connections[backplaneMessage.ConnectionId];
        if (connection is null)
            return;

        bool expectsResult = backplaneMessage.InvocationId is not null || backplaneMessage.ResultNodeId is not null;
        if (expectsResult && (backplaneMessage.InvocationId is null || backplaneMessage.ResultNodeId is null))
            throw new InvalidDataException("A client-result invocation must identify both its invocation and result node.");

        try
        {
            if (expectsResult)
            {
                if (!_lifetimeManager.RegisterRemoteInvocation(
                        connection,
                        backplaneMessage.InvocationId!,
                        backplaneMessage.ResultNodeId!))
                {
                    return;
                }
            }

            SerializedHubMessage invocation = backplaneMessage.ProtocolPayloads.ToSerializedHubMessage();
            await connection.WriteAsync(invocation).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            if (expectsResult)
            {
                await _lifetimeManager.FailRemoteInvocationAsync(
                        backplaneMessage.ConnectionId,
                        backplaneMessage.InvocationId!,
                        "The owning server could not write the invocation to the SignalR connection.")
                    .ConfigureAwait(false);
            }

            LogContext.Warning?.Log(
                exception,
                "A SignalR connection invocation could not be written to connection {ConnectionId}.",
                backplaneMessage.ConnectionId);
        }
    }
}
