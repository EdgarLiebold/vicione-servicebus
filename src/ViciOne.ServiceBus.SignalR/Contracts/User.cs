using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;

namespace ViciOne.ServiceBus.SignalR.Contracts;

/// <summary>
/// Defines the contract for user.
/// </summary>
/// <typeparam name="THub">The t hub type.</typeparam>
public interface User<THub>
    where THub : Hub
{
    /// <summary>
    /// Gets the user id value.
    /// </summary>
    string UserId { get; }
    /// <summary>
    /// Gets the messages value.
    /// </summary>
    IReadOnlyDictionary<string, byte[]> Messages { get; }
}
