using Microsoft.AspNetCore.SignalR;

namespace ViciOne.ServiceBus.SignalR.Scoping;

public interface IHubLifetimeScopeProvider
{
    IHubLifetimeScope<THub> CreateScope<THub>()
        where THub : Hub;
}
