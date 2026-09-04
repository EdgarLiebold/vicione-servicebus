using System;
using Microsoft.AspNetCore.SignalR;
using ViciOne.ServiceBus.SignalR.Contracts;

namespace ViciOne.ServiceBus.SignalR.Scoping;

/// <summary>
/// Defines the contract for hub lifetime scope.
/// </summary>
/// <typeparam name="THub">The t hub type.</typeparam>
public interface IHubLifetimeScope<THub> :
    IAsyncDisposable
    where THub : Hub
{
    /// <summary>
    /// Gets the publish endpoint value.
    /// </summary>
    IPublishEndpoint PublishEndpoint { get; }
    /// <summary>
    /// Gets the request client value.
    /// </summary>
    IRequestClient<GroupManagement<THub>> RequestClient { get; }
}
