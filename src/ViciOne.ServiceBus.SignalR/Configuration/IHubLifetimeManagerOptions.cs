using Microsoft.AspNetCore.SignalR;

namespace ViciOne.ServiceBus.SignalR;

/// <summary>
/// Defines the contract for hub lifetime manager options.
/// </summary>
public interface IHubLifetimeManagerOptions
{
    /// <summary>
    /// Gets or sets the server name value.
    /// </summary>
    string ServerName { set; }
    /// <summary>
    /// Gets or sets the request timeout value.
    /// </summary>
    RequestTimeout RequestTimeout { set; }
}


/// <summary>
/// Defines the contract for hub lifetime manager options.
/// </summary>
/// <typeparam name="THub">The t hub type.</typeparam>
public interface IHubLifetimeManagerOptions<THub> :
    IHubLifetimeManagerOptions
    where THub : Hub
{
}
