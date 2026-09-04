using Microsoft.AspNetCore.SignalR;

namespace ViciOne.ServiceBus.SignalR.Contracts;

/// <summary>
/// Defines the contract for group management.
/// </summary>
/// <typeparam name="THub">The t hub type.</typeparam>
public interface GroupManagement<THub>
    where THub : Hub
{
    /// <summary>
    /// Gets the ServerName of the group command.
    /// </summary>
    string ServerName { get; }

    /// <summary>
    /// Gets the action to be performed on the group.
    /// </summary>
    GroupAction Action { get; }

    /// <summary>
    /// Gets the group on which the action is performed.
    /// </summary>
    string GroupName { get; }

    /// <summary>
    /// Gets the ID of the connection to be added or removed from the group.
    /// </summary>
    string ConnectionId { get; }
}
