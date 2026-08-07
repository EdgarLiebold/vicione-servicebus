// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.SignalR
{
    using Microsoft.AspNetCore.SignalR;


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
}
