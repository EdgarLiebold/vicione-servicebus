// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.SignalR
{
    using System;
    using Configuration.Definitions;
    using Consumers;
    using Contracts;
    using Microsoft.AspNetCore.SignalR;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Scoping;


    public static class ViciOneServiceBusSignalRConfigurationExtensions
    {
        public static void AddSignalRHub<THub>(this IBusRegistrationConfigurator busConfigurator,
            Action<IHubLifetimeManagerOptions<THub>> configureHubLifetimeOptions = null)
            where THub : Hub
        {
            var options = new HubLifetimeManagerOptions<THub>();
            configureHubLifetimeOptions?.Invoke(options);

            busConfigurator.TryAddSingleton<IHubLifetimeScopeProvider, DependencyInjectionHubLifetimeScopeProvider>();

            busConfigurator.AddSingleton(provider => GetViciOneServiceBusHubLifetimeManager(provider, options));
            busConfigurator.AddSingleton<HubLifetimeManager<THub>>(sp => sp.GetRequiredService<ViciOneServiceBusHubLifetimeManager<THub>>());

            busConfigurator.AddRequestClient<GroupManagement<THub>>(options.RequestTimeout);

            RegisterConsumers<THub>(busConfigurator);
        }

        static void RegisterConsumers<THub>(IRegistrationConfigurator configurator)
            where THub : Hub
        {
            configurator.AddSingleton<HubConsumerDefinition<THub>>();

            configurator.TryAddSingleton<IConsumerDefinition<AllConsumer<THub>>, AllConsumerDefinition<THub>>();
            configurator.TryAddSingleton<IConsumerDefinition<ConnectionConsumer<THub>>, ConnectionConsumerDefinition<THub>>();
            configurator.TryAddSingleton<IConsumerDefinition<GroupConsumer<THub>>, GroupConsumerDefinition<THub>>();
            configurator.TryAddSingleton<IConsumerDefinition<GroupManagementConsumer<THub>>, GroupManagementConsumerDefinition<THub>>();
            configurator.TryAddSingleton<IConsumerDefinition<UserConsumer<THub>>, UserConsumerDefinition<THub>>();

            configurator.AddConsumer<AllConsumer<THub>>();
            configurator.AddConsumer<ConnectionConsumer<THub>>();
            configurator.AddConsumer<GroupConsumer<THub>>();
            configurator.AddConsumer<GroupManagementConsumer<THub>>();
            configurator.AddConsumer<UserConsumer<THub>>();
        }

        static ViciOneServiceBusHubLifetimeManager<THub> GetViciOneServiceBusHubLifetimeManager<THub>(IServiceProvider provider, HubLifetimeManagerOptions<THub> options)
            where THub : Hub
        {
            var scopeProvider = provider.GetRequiredService<IHubLifetimeScopeProvider>();
            var resolver = provider.GetRequiredService<IHubProtocolResolver>();
            return new ViciOneServiceBusHubLifetimeManager<THub>(options, scopeProvider, resolver);
        }
    }
}
