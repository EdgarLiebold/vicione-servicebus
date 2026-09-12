using Microsoft.AspNetCore.SignalR;

namespace ViciOne.ServiceBus.SignalR.Contracts;

/// <summary>Carries one serialized invocation to every local member of a named group.</summary>
internal sealed class GroupMessage<THub>
    where THub : Hub
{
    /// <summary>Initializes a group invocation with exact connection exclusions.</summary>
    public GroupMessage(
        string groupName,
        IReadOnlyDictionary<string, byte[]> protocolPayloads,
        string[] excludedConnectionIds)
    {
        GroupName = groupName ?? throw new ArgumentNullException(nameof(groupName));
        ProtocolPayloads = protocolPayloads ?? throw new ArgumentNullException(nameof(protocolPayloads));
        ExcludedConnectionIds = excludedConnectionIds ?? throw new ArgumentNullException(nameof(excludedConnectionIds));
    }

    /// <summary>Gets the case-sensitive SignalR group name.</summary>
    public string GroupName { get; }

    /// <summary>Gets connection identifiers that must not receive this invocation.</summary>
    public string[] ExcludedConnectionIds { get; }

    /// <summary>Gets the invocation serialized once for each protocol supported by the publishing server.</summary>
    public IReadOnlyDictionary<string, byte[]> ProtocolPayloads { get; }
}
