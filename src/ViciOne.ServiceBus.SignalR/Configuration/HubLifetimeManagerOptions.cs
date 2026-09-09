using System;
using Microsoft.AspNetCore.SignalR;
using ViciOne.ServiceBus.SignalR.Utils;

namespace ViciOne.ServiceBus.SignalR;

/// <summary>Defines configuration options for hub lifetime manager.</summary>
/// <typeparam name="THub">The hub type.</typeparam>
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

    /// <summary>Initializes a new instance.</summary>
    public HubLifetimeManagerOptions()
    {
        ServerName = $"{Environment.MachineName}_{NewId.NextGuid():N}";
        RequestTimeout = new RequestTimeout(TimeSpan.FromSeconds(20));
        ConnectionStore = new HubConnectionStore();
        GroupsSubscriptionManager = new ViciOneServiceBusSubscriptionManager();
        UsersSubscriptionManager = new ViciOneServiceBusSubscriptionManager();
    }

    /// <summary>Gets the connection store.</summary>
    public HubConnectionStore ConnectionStore { get; }
    /// <summary>Gets the groups subscription manager.</summary>
    public ViciOneServiceBusSubscriptionManager GroupsSubscriptionManager { get; }
    /// <summary>Gets the users subscription manager.</summary>
    public ViciOneServiceBusSubscriptionManager UsersSubscriptionManager { get; }

    /// <summary>Gets or sets the server name.</summary>
    public string ServerName { get; set; }
    /// <summary>Gets or sets the request timeout.</summary>
    public RequestTimeout RequestTimeout { get; set; }
}
