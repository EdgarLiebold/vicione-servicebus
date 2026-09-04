using Microsoft.AspNetCore.SignalR;

namespace ViciOne.ServiceBus.SignalR.Contracts;

public interface Ack<THub>
    where THub : Hub
{
    string ServerName { get; }
}
