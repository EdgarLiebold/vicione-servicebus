using Microsoft.AspNetCore.SignalR;

namespace ViciOne.ServiceBus.SignalR.Contracts;

/// <summary>Carries one serialized hub invocation to every server hosting the same hub.</summary>
internal sealed class BroadcastMessage<THub>
    where THub : Hub
{
    /// <summary>Initializes a broadcast with protocol-specific payloads and exact connection exclusions.</summary>
    public BroadcastMessage(IReadOnlyDictionary<string, byte[]> protocolPayloads, string[] excludedConnectionIds)
    {
        ProtocolPayloads = protocolPayloads ?? throw new ArgumentNullException(nameof(protocolPayloads));
        ExcludedConnectionIds = excludedConnectionIds ?? throw new ArgumentNullException(nameof(excludedConnectionIds));
    }

    /// <summary>Gets connection identifiers that must not receive this invocation.</summary>
    public string[] ExcludedConnectionIds { get; }

    /// <summary>Gets the invocation serialized once for each protocol supported by the publishing server.</summary>
    public IReadOnlyDictionary<string, byte[]> ProtocolPayloads { get; }
}
