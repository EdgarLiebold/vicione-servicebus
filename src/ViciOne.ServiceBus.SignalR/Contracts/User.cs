using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;

namespace ViciOne.ServiceBus.SignalR.Contracts;

public interface User<THub>
    where THub : Hub
{
    string UserId { get; }
    IReadOnlyDictionary<string, byte[]> Messages { get; }
}
