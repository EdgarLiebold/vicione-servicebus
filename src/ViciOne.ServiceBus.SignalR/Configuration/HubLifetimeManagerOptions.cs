using System;
using Microsoft.AspNetCore.SignalR;
using ViciOne.ServiceBus.SignalR.Utils;

namespace ViciOne.ServiceBus.SignalR;

/// <summary>
/// Defines configuration options for hub lifetime manager.
/// </summary>
/// <typeparam name="THub">The t hub type.</typeparam>
public sealed class HubLifetimeManagerOptions<THub> :
    IHubLifetimeManagerOptions<THub>
    where THub : Hub
{
    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(ServerName))
        {
            throw new ConfigurationException(
                "SignalR hub lifetime for bus 'default': ServerName must not be empty. Set a stable non-empty server name.");
        }
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public HubLifetimeManagerOptions()
    {
        ServerName = $"{Environment.MachineName}_{NewId.NextGuid():N}";
        RequestTimeout = TimeSpan.FromSeconds(20);
        ConnectionStore = new HubConnectionStore();
        GroupsSubscriptionManager = new ViciOneServiceBusSubscriptionManager();
        UsersSubscriptionManager = new ViciOneServiceBusSubscriptionManager();
    }

    /// <summary>
    /// Gets the connection store value.
    /// </summary>
    public HubConnectionStore ConnectionStore { get; }
    /// <summary>
    /// Gets the groups subscription manager value.
    /// </summary>
    public ViciOneServiceBusSubscriptionManager GroupsSubscriptionManager { get; }
    /// <summary>
    /// Gets the users subscription manager value.
    /// </summary>
    public ViciOneServiceBusSubscriptionManager UsersSubscriptionManager { get; }

    /// <summary>
    /// Gets or sets the server name value.
    /// </summary>
    public string ServerName { get; set; }
    /// <summary>
    /// Gets or sets the request timeout value.
    /// </summary>
    public RequestTimeout RequestTimeout { get; set; }
}
