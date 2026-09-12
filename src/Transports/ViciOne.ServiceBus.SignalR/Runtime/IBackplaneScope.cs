using System;
using Microsoft.AspNetCore.SignalR;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.SignalR.Contracts;

namespace ViciOne.ServiceBus.SignalR.Runtime;

/// <summary>Provides the scoped bus services used by one SignalR backplane operation.</summary>
internal interface IBackplaneScope<THub> :
    IAsyncDisposable
    where THub : Hub
{
    /// <summary>Gets the endpoint used to publish routed hub messages.</summary>
    IPublishEndpoint PublishEndpoint { get; }

    /// <summary>Gets the client used for acknowledged remote group commands.</summary>
    IRequestClient<GroupCommand<THub>> GroupCommandClient { get; }
}
