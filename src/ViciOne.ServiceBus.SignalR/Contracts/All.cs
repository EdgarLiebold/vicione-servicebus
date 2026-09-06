using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;

namespace ViciOne.ServiceBus.SignalR.Contracts;

/// <summary>Defines the operations required by all.</summary>
/// <typeparam name="THub">The hub type.</typeparam>
public interface All<THub>
    where THub : Hub
{
    /// <summary>Gets the excluded connection ids.</summary>
    string[] ExcludedConnectionIds { get; }
    /// <summary>Gets the messages.</summary>
    IReadOnlyDictionary<string, byte[]> Messages { get; }
}
