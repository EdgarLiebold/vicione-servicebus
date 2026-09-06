using Microsoft.AspNetCore.SignalR;

namespace ViciOne.ServiceBus.SignalR;

/// <summary>Defines configuration options for hub lifetime manager.</summary>
public interface IHubLifetimeManagerOptions
{
    /// <summary>Gets or sets the server name.</summary>
    string ServerName { set; }
    /// <summary>Gets or sets the request timeout.</summary>
    RequestTimeout RequestTimeout { set; }
}


/// <summary>Defines configuration options for hub lifetime manager.</summary>
/// <typeparam name="THub">The hub type.</typeparam>
public interface IHubLifetimeManagerOptions<THub> :
    IHubLifetimeManagerOptions
    where THub : Hub
{
}
