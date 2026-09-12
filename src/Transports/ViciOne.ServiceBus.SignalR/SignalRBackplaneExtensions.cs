using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Providers.Configuration;
using ViciOne.ServiceBus.SignalR.Configuration;
using ViciOne.ServiceBus.SignalR.Consumers;
using ViciOne.ServiceBus.SignalR.Contracts;
using ViciOne.ServiceBus.SignalR.Runtime;

namespace ViciOne.ServiceBus.SignalR;

/// <summary>Provides registration for SignalR scale-out over ViciOne.ServiceBus.</summary>
public static class SignalRBackplaneExtensions
{
    /// <summary>Registers a bus-backed lifetime manager and its isolated backplane endpoint for one hub type.</summary>
    /// <typeparam name="THub">The SignalR hub whose connections are scaled across application nodes.</typeparam>
    /// <param name="configurator">The bus registration being composed.</param>
    /// <param name="configure">An optional callback that configures remote group operations.</param>
    /// <returns>The same registration configurator for fluent composition.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="configurator" /> is <see langword="null" />.</exception>
    /// <exception cref="ConfigurationException">The hub is already registered or its options are invalid.</exception>
    public static IBusRegistrationConfigurator AddSignalRBackplane<THub>(
        this IBusRegistrationConfigurator configurator,
        Action<SignalRBackplaneOptions>? configure = null)
        where THub : Hub
    {
        ArgumentNullException.ThrowIfNull(configurator);

        var options = new SignalRBackplaneOptions();
        configure?.Invoke(options);
        options.Validate();

        Type settingsType = typeof(SignalRBackplaneSettings<THub>);
        if (configurator.Services.Any(descriptor => descriptor.ServiceType == settingsType))
        {
            throw new ConfigurationException(ConfigurationMessages.Create(
                "SignalR backplane",
                "registration",
                $"Hub '{typeof(THub)}' already has a backplane registration",
                "Register each hub type exactly once"));
        }

        var settings = new SignalRBackplaneSettings<THub>(
            new RequestTimeout(options.RemoteGroupOperationTimeout));

        configurator.Services.AddSingleton(settings);
        configurator.Services.TryAddSingleton<IBackplaneScopeProvider, DependencyInjectionBackplaneScopeProvider>();
        configurator.Services.TryAddSingleton<SignalRBackplaneEndpointDefinition<THub>>();
        configurator.Services.AddSingleton<ServiceBusHubLifetimeManager<THub>>();
        configurator.Services.AddSingleton<HubLifetimeManager<THub>>(provider =>
            provider.GetRequiredService<ServiceBusHubLifetimeManager<THub>>());

        configurator.AddRequestClient<GroupCommand<THub>>(settings.RemoteGroupOperationTimeout);
        RegisterConsumers<THub>(configurator);

        return configurator;
    }

    static void RegisterConsumers<THub>(IRegistrationConfigurator configurator)
        where THub : Hub
    {
        configurator.AddConsumer<BroadcastConsumer<THub>, BackplaneConsumerDefinition<BroadcastConsumer<THub>, THub>>();
        configurator.AddConsumer<ConnectionConsumer<THub>, BackplaneConsumerDefinition<ConnectionConsumer<THub>, THub>>();
        configurator.AddConsumer<GroupConsumer<THub>, BackplaneConsumerDefinition<GroupConsumer<THub>, THub>>();
        configurator.AddConsumer<GroupCommandConsumer<THub>, BackplaneConsumerDefinition<GroupCommandConsumer<THub>, THub>>();
        configurator.AddConsumer<UserConsumer<THub>, BackplaneConsumerDefinition<UserConsumer<THub>, THub>>();
        configurator.AddConsumer<ClientResultConsumer<THub>, BackplaneConsumerDefinition<ClientResultConsumer<THub>, THub>>();
        configurator.AddConsumer<InvocationCancellationConsumer<THub>, BackplaneConsumerDefinition<InvocationCancellationConsumer<THub>, THub>>();
    }
}
