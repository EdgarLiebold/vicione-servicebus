// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.SignalR.Scoping
{
    using System;
    using Contracts;
    using Microsoft.AspNetCore.SignalR;


    public interface IHubLifetimeScope<THub> :
        IAsyncDisposable
        where THub : Hub
    {
        IPublishEndpoint PublishEndpoint { get; }
        IRequestClient<GroupManagement<THub>> RequestClient { get; }
    }
}
