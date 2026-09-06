using System;
using Microsoft.AspNetCore.SignalR;
using ViciOne.ServiceBus.SignalR.Contracts;

namespace ViciOne.ServiceBus.SignalR.Scoping;

/// <summary>Defines the operations required by hub lifetime scope.</summary>
/// <typeparam name="THub">The hub type.</typeparam>
public interface IHubLifetimeScope<THub> :
    IAsyncDisposable
    where THub : Hub
{
    /// <summary>Gets the publish endpoint.</summary>
    IPublishEndpoint PublishEndpoint { get; }
    /// <summary>Gets the request client.</summary>
    IRequestClient<GroupManagement<THub>> RequestClient { get; }
}
