using Microsoft.AspNetCore.SignalR;

namespace ViciOne.ServiceBus.SignalR.Scoping;

/// <summary>Provides hub lifetime scope services.</summary>
public interface IHubLifetimeScopeProvider
{
    /// <summary>Creates scope.</summary>
    /// <typeparam name="THub">The hub type.</typeparam>
    /// <returns>The created scope.</returns>
    IHubLifetimeScope<THub> CreateScope<THub>()
        where THub : Hub;
}
