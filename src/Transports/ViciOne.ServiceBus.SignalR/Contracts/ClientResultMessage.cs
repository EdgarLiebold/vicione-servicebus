using Microsoft.AspNetCore.SignalR;

namespace ViciOne.ServiceBus.SignalR.Contracts;

/// <summary>Returns a protocol-encoded client completion to the node that initiated an invocation.</summary>
internal sealed class ClientResultMessage<THub>
    where THub : Hub
{
    /// <summary>Initializes a routed client completion.</summary>
    public ClientResultMessage(string targetNodeId, string invocationId, string protocolName, byte[] payload)
    {
        TargetNodeId = targetNodeId ?? throw new ArgumentNullException(nameof(targetNodeId));
        InvocationId = invocationId ?? throw new ArgumentNullException(nameof(invocationId));
        ProtocolName = protocolName ?? throw new ArgumentNullException(nameof(protocolName));
        Payload = payload ?? throw new ArgumentNullException(nameof(payload));
    }

    /// <summary>Gets the node that owns the pending invocation.</summary>
    public string TargetNodeId { get; }

    /// <summary>Gets the invocation completed by the payload.</summary>
    public string InvocationId { get; }

    /// <summary>Gets the SignalR protocol used to encode the completion.</summary>
    public string ProtocolName { get; }

    /// <summary>Gets the protocol frame containing the completion.</summary>
    public byte[] Payload { get; }
}
