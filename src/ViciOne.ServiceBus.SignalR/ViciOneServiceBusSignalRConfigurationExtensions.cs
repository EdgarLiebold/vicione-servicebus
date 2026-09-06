using System;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.SignalR.Configuration.Definitions;
using ViciOne.ServiceBus.SignalR.Consumers;
using ViciOne.ServiceBus.SignalR.Contracts;
using ViciOne.ServiceBus.SignalR.Scoping;

namespace ViciOne.ServiceBus.SignalR;

/// <summary>Provides extension methods for vici one service bus signal r configuration.</summary>
public static class ViciOneServiceBusSignalRConfigurationExtensions
{
    /// <summary>Adds signal r hub to the configuration.</summary>
    /// <typeparam name="THub">The hub type.</typeparam>
    /// <param name="busConfigurator">The bus configurator.</param>
    /// <param name="configureHubLifetimeOptions">The configure hub lifetime options.</param>
    public static void AddSignalRHub<THub>(this IBusRegistrationConfigurator busConfigurator,
        Action<IHubLifetimeManagerOptions<THub>>? configureHubLifetimeOptions = null)
        where THub : Hub
    {
        var options = new HubLifetimeManagerOptions<THub>();
        configureHubLifetimeOptions?.Invoke(options);
        options.Validate();

        busConfigurator.Services.TryAddSingleton<IHubLifetimeScopeProvider, DependencyInjectionHubLifetimeScopeProvider>();

        busConfigurator.Services.AddSingleton(provider => GetViciOneServiceBusHubLifetimeManager(provider, options));
        busConfigurator.Services.AddSingleton<HubLifetimeManager<THub>>(sp => sp.GetRequiredService<ViciOneServiceBusHubLifetimeManager<THub>>());

        busConfigurator.AddRequestClient<GroupManagement<THub>>(options.RequestTimeout);

        RegisterConsumers<THub>(busConfigurator);
    }

    static void RegisterConsumers<THub>(IRegistrationConfigurator configurator)
        where THub : Hub
    {
        configurator.Services.AddSingleton<HubConsumerDefinition<THub>>();

        configurator.Services.TryAddSingleton<IConsumerDefinition<AllConsumer<THub>>, AllConsumerDefinition<THub>>();
        configurator.Services.TryAddSingleton<IConsumerDefinition<ConnectionConsumer<THub>>, ConnectionConsumerDefinition<THub>>();
        configurator.Services.TryAddSingleton<IConsumerDefinition<GroupConsumer<THub>>, GroupConsumerDefinition<THub>>();
        configurator.Services.TryAddSingleton<IConsumerDefinition<GroupManagementConsumer<THub>>, GroupManagementConsumerDefinition<THub>>();
        configurator.Services.TryAddSingleton<IConsumerDefinition<UserConsumer<THub>>, UserConsumerDefinition<THub>>();

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
