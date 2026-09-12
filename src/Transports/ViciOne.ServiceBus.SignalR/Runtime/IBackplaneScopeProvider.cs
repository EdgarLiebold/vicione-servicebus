using Microsoft.AspNetCore.SignalR;

namespace ViciOne.ServiceBus.SignalR.Runtime;

/// <summary>Creates an isolated dependency-injection scope for each backplane operation.</summary>
internal interface IBackplaneScopeProvider
{
    /// <summary>Creates a scope containing the publish endpoint and group-command client for a hub.</summary>
    ValueTask<IBackplaneScope<THub>> CreateScopeAsync<THub>()
        where THub : Hub;
}
