using System;
using Microsoft.AspNetCore.SignalR;
using ViciOne.ServiceBus.SignalR.Contracts;

namespace ViciOne.ServiceBus.SignalR.Scoping;

public interface IHubLifetimeScope<THub> :
    IAsyncDisposable
    where THub : Hub
{
    IPublishEndpoint PublishEndpoint { get; }
    IRequestClient<GroupManagement<THub>> RequestClient { get; }
}
