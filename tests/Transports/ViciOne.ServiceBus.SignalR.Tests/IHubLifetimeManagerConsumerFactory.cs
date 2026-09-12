using Microsoft.AspNetCore.SignalR;
using ViciOne.ServiceBus.SignalR.Runtime;

namespace ViciOne.ServiceBus.SignalR.Tests;

internal interface IHubLifetimeManagerConsumerFactory<THub>
    where THub : Hub
{
    ServiceBusHubLifetimeManager<THub> Manager { set; }
}
