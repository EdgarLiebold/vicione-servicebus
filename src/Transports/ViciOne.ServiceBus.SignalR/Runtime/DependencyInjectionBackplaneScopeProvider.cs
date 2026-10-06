using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.SignalR.Configuration;
using ViciOne.ServiceBus.SignalR.Contracts;

namespace ViciOne.ServiceBus.SignalR.Runtime;

/// <summary>Resolves scoped bus collaborators without retaining them in the singleton lifetime manager.</summary>
internal sealed class DependencyInjectionBackplaneScopeProvider :
    IBackplaneScopeProvider
{
    readonly IServiceScopeFactory _serviceScopeFactory;

    /// <summary>Initializes the provider with the application's scope factory.</summary>
    public DependencyInjectionBackplaneScopeProvider(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
    }

    /// <summary>Creates and fully resolves one operation scope.</summary>
    public async ValueTask<IBackplaneScope<THub>> CreateScopeAsync<THub>()
        where THub : Hub
    {
        AsyncServiceScope serviceScope = _serviceScopeFactory.CreateAsyncScope();
        try
        {
            Type busType = serviceScope.ServiceProvider.GetService<SignalRBackplaneSettings<THub>>()?.BusType ?? typeof(IBus);
            return ScopeFactories<THub>.Get(busType)(serviceScope);
        }
        catch (Exception operationFailure)
        {
            await BackplaneOperationLifetime.ReleaseAfterOperationAsync(serviceScope, null, operationFailure)
                .ConfigureAwait(false);
            throw;
        }
    }

    static IBackplaneScope<THub> CreateDefaultScope<THub>(AsyncServiceScope serviceScope)
        where THub : Hub
    {
        IServiceProvider provider = serviceScope.ServiceProvider;
        return new BackplaneScope<THub>(serviceScope,
            provider.GetRequiredService<IPublishEndpoint>(),
            provider.GetRequiredService<IRequestClient<GroupCommand<THub>>>());
    }

    static IBackplaneScope<THub> CreateOwnedScope<TBus, THub>(AsyncServiceScope serviceScope)
        where TBus : class, IBus
        where THub : Hub
    {
        IServiceProvider provider = serviceScope.ServiceProvider;
        return new BackplaneScope<THub>(serviceScope,
            provider.GetRequiredService<Bind<TBus, IPublishEndpoint>>().Value,
            provider.GetRequiredService<Bind<TBus, IRequestClient<GroupCommand<THub>>>>().Value);
    }

    static class ScopeFactories<THub>
        where THub : Hub
    {
        static readonly ConcurrentDictionary<Type, Func<AsyncServiceScope, IBackplaneScope<THub>>> _factories = new();

        public static Func<AsyncServiceScope, IBackplaneScope<THub>> Get(Type busType)
        {
            if (busType == typeof(IBus))
                return CreateDefaultScope<THub>;

            return _factories.GetOrAdd(busType, static type =>
                typeof(DependencyInjectionBackplaneScopeProvider)
                    .GetMethod(nameof(CreateOwnedScope), BindingFlags.Static | BindingFlags.NonPublic)!
                    .MakeGenericMethod(type, typeof(THub))
                    .CreateDelegate<Func<AsyncServiceScope, IBackplaneScope<THub>>>());
        }
    }

    sealed class BackplaneScope<THub> :
        IBackplaneScope<THub>
        where THub : Hub
    {
        readonly AsyncServiceScope _serviceScope;

        public BackplaneScope(AsyncServiceScope serviceScope, IPublishEndpoint publishEndpoint,
            IRequestClient<GroupCommand<THub>> groupCommandClient)
        {
            _serviceScope = serviceScope;
            PublishEndpoint = publishEndpoint;
            GroupCommandClient = groupCommandClient;
        }

        public IPublishEndpoint PublishEndpoint { get; }

        public IRequestClient<GroupCommand<THub>> GroupCommandClient { get; }

        public ValueTask DisposeAsync()
        {
            return _serviceScope.DisposeAsync();
        }
    }
}
