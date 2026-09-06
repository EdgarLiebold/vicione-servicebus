using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;

namespace ViciOne.ServiceBus.SignalR.Contracts;

/// <summary>Defines the operations required by user.</summary>
/// <typeparam name="THub">The hub type.</typeparam>
public interface User<THub>
    where THub : Hub
{
    /// <summary>Gets the user id.</summary>
    string UserId { get; }
    /// <summary>Gets the messages.</summary>
    IReadOnlyDictionary<string, byte[]> Messages { get; }
}
