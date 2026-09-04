using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;

namespace ViciOne.ServiceBus.SignalR.Contracts;

/// <summary>
/// Defines the contract for all.
/// </summary>
/// <typeparam name="THub">The t hub type.</typeparam>
public interface All<THub>
    where THub : Hub
{
    /// <summary>
    /// Gets the excluded connection ids value.
    /// </summary>
    string[] ExcludedConnectionIds { get; }
    /// <summary>
    /// Gets the messages value.
    /// </summary>
    IReadOnlyDictionary<string, byte[]> Messages { get; }
}
