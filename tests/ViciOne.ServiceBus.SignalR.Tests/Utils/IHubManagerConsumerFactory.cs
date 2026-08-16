namespace ViciOne.ServiceBus.SignalR.Tests.Utils
{
    using Microsoft.AspNetCore.SignalR;


    public interface IHubManagerConsumerFactory<THub>
        where THub : Hub
    {
        ViciOneServiceBusHubLifetimeManager<THub> HubLifetimeManager { get; set; }
    }
}
