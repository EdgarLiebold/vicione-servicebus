using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;

namespace ViciOne.ServiceBus.SignalR.Contracts;

public interface Group<THub>
    where THub : Hub
{
    string GroupName { get; }
    string[] ExcludedConnectionIds { get; }
    IReadOnlyDictionary<string, byte[]> Messages { get; }
}
