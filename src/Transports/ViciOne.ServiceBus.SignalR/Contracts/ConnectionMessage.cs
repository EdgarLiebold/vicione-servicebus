using Microsoft.AspNetCore.SignalR;

namespace ViciOne.ServiceBus.SignalR.Contracts;

/// <summary>Carries one serialized invocation to the server that owns a specific connection.</summary>
internal sealed class ConnectionMessage<THub>
    where THub : Hub
{
    /// <summary>Initializes a routed connection invocation.</summary>
    public ConnectionMessage(
        string connectionId,
        IReadOnlyDictionary<string, byte[]> protocolPayloads,
        string? invocationId = null,
        string? resultNodeId = null)
    {
        ConnectionId = connectionId ?? throw new ArgumentNullException(nameof(connectionId));
        ProtocolPayloads = protocolPayloads ?? throw new ArgumentNullException(nameof(protocolPayloads));
        InvocationId = invocationId;
        ResultNodeId = resultNodeId;
    }

    /// <summary>Gets the exact SignalR connection identifier.</summary>
    public string ConnectionId { get; }

    /// <summary>Gets the client-result invocation identifier, or <see langword="null" /> for a one-way send.</summary>
    public string? InvocationId { get; }

    /// <summary>Gets the backplane node that awaits the client result, or <see langword="null" /> for a one-way send.</summary>
    public string? ResultNodeId { get; }

    /// <summary>Gets the invocation serialized once for each protocol supported by the publishing server.</summary>
    public IReadOnlyDictionary<string, byte[]> ProtocolPayloads { get; }
}
