using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;

namespace ViciOne.ServiceBus.SignalR.Contracts;

/// <summary>
/// Defines the contract for connection.
/// </summary>
/// <typeparam name="THub">The t hub type.</typeparam>
public interface Connection<THub>
    where THub : Hub
{
    /// <summary>
    /// Gets the connection id value.
    /// </summary>
    string ConnectionId { get; }
    /// <summary>
    /// Gets the messages value.
    /// </summary>
    IReadOnlyDictionary<string, byte[]> Messages { get; }
}
