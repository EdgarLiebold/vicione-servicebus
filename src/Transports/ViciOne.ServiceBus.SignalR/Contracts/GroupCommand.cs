using Microsoft.AspNetCore.SignalR;

namespace ViciOne.ServiceBus.SignalR.Contracts;

/// <summary>Requests a group-membership change from the server that owns a connection.</summary>
internal sealed class GroupCommand<THub>
    where THub : Hub
{
    /// <summary>Initializes a command addressed by connection ownership.</summary>
    public GroupCommand(GroupCommandAction action, string groupName, string connectionId)
    {
        Action = action;
        GroupName = groupName ?? throw new ArgumentNullException(nameof(groupName));
        ConnectionId = connectionId ?? throw new ArgumentNullException(nameof(connectionId));
    }

    /// <summary>Gets the membership operation to apply.</summary>
    public GroupCommandAction Action { get; }

    /// <summary>Gets the case-sensitive SignalR group name.</summary>
    public string GroupName { get; }

    /// <summary>Gets the exact SignalR connection identifier.</summary>
    public string ConnectionId { get; }
}
