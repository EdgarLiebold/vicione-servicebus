using Microsoft.AspNetCore.SignalR;

namespace ViciOne.ServiceBus.SignalR;

public interface IHubLifetimeManagerOptions
{
    string ServerName { set; }
    RequestTimeout RequestTimeout { set; }
}


public interface IHubLifetimeManagerOptions<THub> :
    IHubLifetimeManagerOptions
    where THub : Hub
{
}
