// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.SignalR
{
    using System;
    using Microsoft.AspNetCore.SignalR;
    using Utils;


    public class HubLifetimeManagerOptions<THub> :
        IHubLifetimeManagerOptions<THub>
        where THub : Hub
    {
        public HubLifetimeManagerOptions()
        {
            ServerName = $"{Environment.MachineName}_{NewId.NextGuid():N}";
            RequestTimeout = TimeSpan.FromSeconds(20);
            ConnectionStore = new HubConnectionStore();
            GroupsSubscriptionManager = new ViciOneServiceBusSubscriptionManager();
            UsersSubscriptionManager = new ViciOneServiceBusSubscriptionManager();
        }

        public HubConnectionStore ConnectionStore { get; }
        public ViciOneServiceBusSubscriptionManager GroupsSubscriptionManager { get; }
        public ViciOneServiceBusSubscriptionManager UsersSubscriptionManager { get; }

        public string ServerName { get; set; }
        public RequestTimeout RequestTimeout { get; set; }
    }
}
