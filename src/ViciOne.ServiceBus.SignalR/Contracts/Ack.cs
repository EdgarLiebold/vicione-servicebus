// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.SignalR.Contracts
{
    using Microsoft.AspNetCore.SignalR;


    public interface Ack<THub>
        where THub : Hub
    {
        string ServerName { get; }
    }
}
