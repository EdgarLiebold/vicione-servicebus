// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.SignalR.Tests.Utils
{
    using Microsoft.AspNetCore.SignalR;


    public interface IHubManagerConsumerFactory<THub>
        where THub : Hub
    {
        ViciOneServiceBusHubLifetimeManager<THub> HubLifetimeManager { get; set; }
    }
}
