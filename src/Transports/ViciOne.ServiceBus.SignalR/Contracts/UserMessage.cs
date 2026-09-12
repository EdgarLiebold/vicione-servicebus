using Microsoft.AspNetCore.SignalR;

namespace ViciOne.ServiceBus.SignalR.Contracts;

/// <summary>Carries one serialized invocation to every local connection for a user.</summary>
internal sealed class UserMessage<THub>
    where THub : Hub
{
    /// <summary>Initializes a user-targeted invocation.</summary>
    public UserMessage(string userId, IReadOnlyDictionary<string, byte[]> protocolPayloads)
    {
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        ProtocolPayloads = protocolPayloads ?? throw new ArgumentNullException(nameof(protocolPayloads));
    }

    /// <summary>Gets the case-sensitive SignalR user identifier.</summary>
    public string UserId { get; }

    /// <summary>Gets the invocation serialized once for each protocol supported by the publishing server.</summary>
    public IReadOnlyDictionary<string, byte[]> ProtocolPayloads { get; }
}
