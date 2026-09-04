using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;

namespace ViciOne.ServiceBus.SignalR.Contracts;

/// <summary>
/// Defines the contract for group.
/// </summary>
/// <typeparam name="THub">The t hub type.</typeparam>
public interface Group<THub>
    where THub : Hub
{
    /// <summary>
    /// Gets the group name value.
    /// </summary>
    string GroupName { get; }
    /// <summary>
    /// Gets the excluded connection ids value.
    /// </summary>
    string[] ExcludedConnectionIds { get; }
    /// <summary>
    /// Gets the messages value.
    /// </summary>
    IReadOnlyDictionary<string, byte[]> Messages { get; }
}
