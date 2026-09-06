using Microsoft.AspNetCore.SignalR;

namespace ViciOne.ServiceBus.SignalR.Contracts;

/// <summary>Defines the operations required by ack.</summary>
/// <typeparam name="THub">The hub type.</typeparam>
public interface Ack<THub>
    where THub : Hub
{
    /// <summary>Gets the server name.</summary>
    string ServerName { get; }
}
