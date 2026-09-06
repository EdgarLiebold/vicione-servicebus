using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;

namespace ViciOne.ServiceBus.SignalR.Contracts;

/// <summary>Defines the operations required by connection.</summary>
/// <typeparam name="THub">The hub type.</typeparam>
public interface Connection<THub>
    where THub : Hub
{
    /// <summary>Gets the connection id.</summary>
    string ConnectionId { get; }
    /// <summary>Gets the messages.</summary>
    IReadOnlyDictionary<string, byte[]> Messages { get; }
}
