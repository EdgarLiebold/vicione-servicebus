using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.SignalR.Contracts;

namespace ViciOne.ServiceBus.SignalR.Scoping;

/// <summary>Provides dependency injection hub lifetime scope services.</summary>
public class DependencyInjectionHubLifetimeScopeProvider :
    IHubLifetimeScopeProvider
{
    readonly IServiceScopeFactory _serviceScopeFactory;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="serviceScopeFactory">The service scope factory.</param>
    public DependencyInjectionHubLifetimeScopeProvider(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    /// <summary>Creates scope.</summary>
    /// <typeparam name="THub">The hub type.</typeparam>
    /// <returns>The created scope.</returns>
    public IHubLifetimeScope<THub> CreateScope<THub>()
        where THub : Hub
    {
        return new HubLifetimeScope<THub>(_serviceScopeFactory.CreateAsyncScope());
    }


    class HubLifetimeScope<THub> :
        IHubLifetimeScope<THub>
        where THub : Hub
    {
        readonly AsyncServiceScope _serviceScope;

        public HubLifetimeScope(AsyncServiceScope serviceScope)
        {
            _serviceScope = serviceScope;
            PublishEndpoint = ServiceProvider.GetRequiredService<IPublishEndpoint>();
            RequestClient = ServiceProvider.GetRequiredService<IRequestClient<GroupManagement<THub>>>();
        }

        IServiceProvider ServiceProvider => _serviceScope.ServiceProvider;

        public IPublishEndpoint PublishEndpoint { get; }
        public IRequestClient<GroupManagement<THub>> RequestClient { get; }

        public ValueTask DisposeAsync()
        {
            return _serviceScope.DisposeAsync();
        }
    }
}
