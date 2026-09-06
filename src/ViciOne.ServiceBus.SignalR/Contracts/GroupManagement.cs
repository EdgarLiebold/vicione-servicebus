using Microsoft.AspNetCore.SignalR;

namespace ViciOne.ServiceBus.SignalR.Contracts;

/// <summary>Defines the operations required by group management.</summary>
/// <typeparam name="THub">The hub type.</typeparam>
public interface GroupManagement<THub>
    where THub : Hub
{
    /// <summary>Gets the server name.</summary>
    string ServerName { get; }

    /// <summary>Gets the action.</summary>
    GroupAction Action { get; }

    /// <summary>Gets the group name.</summary>
    string GroupName { get; }

    /// <summary>Gets the connection id.</summary>
    string ConnectionId { get; }
}
