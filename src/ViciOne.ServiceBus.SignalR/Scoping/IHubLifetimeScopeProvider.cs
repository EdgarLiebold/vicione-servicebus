// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.SignalR.Scoping
{
    using Microsoft.AspNetCore.SignalR;


    public interface IHubLifetimeScopeProvider
    {
        IHubLifetimeScope<THub> CreateScope<THub>()
            where THub : Hub;
    }
}
