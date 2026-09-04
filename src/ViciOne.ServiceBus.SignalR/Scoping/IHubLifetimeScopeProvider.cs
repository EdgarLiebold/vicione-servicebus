using Microsoft.AspNetCore.SignalR;

namespace ViciOne.ServiceBus.SignalR.Scoping;

/// <summary>
/// Defines the contract for hub lifetime scope provider.
/// </summary>
public interface IHubLifetimeScopeProvider
{
    /// <summary>
    /// Creates scope.
    /// </summary>
    /// <typeparam name="THub">The t hub type.</typeparam>
    /// <returns>The result of the operation.</returns>
    IHubLifetimeScope<THub> CreateScope<THub>()
        where THub : Hub;
}
