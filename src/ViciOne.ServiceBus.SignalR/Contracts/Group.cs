using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;

namespace ViciOne.ServiceBus.SignalR.Contracts;

/// <summary>Defines the operations required by group.</summary>
/// <typeparam name="THub">The hub type.</typeparam>
public interface Group<THub>
    where THub : Hub
{
    /// <summary>Gets the group name.</summary>
    string GroupName { get; }
    /// <summary>Gets the excluded connection ids.</summary>
    string[] ExcludedConnectionIds { get; }
    /// <summary>Gets the messages.</summary>
    IReadOnlyDictionary<string, byte[]> Messages { get; }
}
