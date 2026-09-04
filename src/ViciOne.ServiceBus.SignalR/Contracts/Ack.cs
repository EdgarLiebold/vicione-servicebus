using Microsoft.AspNetCore.SignalR;

namespace ViciOne.ServiceBus.SignalR.Contracts;

/// <summary>
/// Defines the contract for ack.
/// </summary>
/// <typeparam name="THub">The t hub type.</typeparam>
public interface Ack<THub>
    where THub : Hub
{
    /// <summary>
    /// Gets the server name value.
    /// </summary>
    string ServerName { get; }
}
