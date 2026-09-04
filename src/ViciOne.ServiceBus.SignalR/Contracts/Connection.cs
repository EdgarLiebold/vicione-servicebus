using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;

namespace ViciOne.ServiceBus.SignalR.Contracts;

public interface Connection<THub>
    where THub : Hub
{
    string ConnectionId { get; }
    IReadOnlyDictionary<string, byte[]> Messages { get; }
}
