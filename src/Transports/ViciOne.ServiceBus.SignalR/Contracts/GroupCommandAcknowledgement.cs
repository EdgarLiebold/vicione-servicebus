using Microsoft.AspNetCore.SignalR;

namespace ViciOne.ServiceBus.SignalR.Contracts;

/// <summary>Confirms that the server owning a connection applied a remote group command.</summary>
internal sealed class GroupCommandAcknowledgement<THub>
    where THub : Hub
{
    /// <summary>Initializes an acknowledgement from the node that handled the command.</summary>
    public GroupCommandAcknowledgement(string nodeId)
    {
        NodeId = nodeId ?? throw new ArgumentNullException(nameof(nodeId));
    }

    /// <summary>Gets the identifier of the node that applied the command.</summary>
    public string NodeId { get; }
}
