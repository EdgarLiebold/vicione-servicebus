using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;

namespace ViciOne.ServiceBus.SignalR.Contracts;

public interface All<THub>
    where THub : Hub
{
    string[] ExcludedConnectionIds { get; }
    IReadOnlyDictionary<string, byte[]> Messages { get; }
}
