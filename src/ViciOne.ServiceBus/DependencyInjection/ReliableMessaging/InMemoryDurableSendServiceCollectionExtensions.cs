using System;
using System.Linq;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Providers.Persistence;
using ViciOne.ServiceBus.Providers.Transports;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Registers process-local persistence and dispatch adapters for reliable messaging.</summary>
public static class InMemoryDurableSendServiceCollectionExtensions
{
    /// <summary>Selects volatile process-local storage that does not preserve messages across host restarts.</summary>
    /// <param name="configurator">The reliable-messaging configurator to update.</param>
    /// <returns>The same configurator for fluent composition.</returns>
    public static IReliableMessagingConfigurator UseInMemoryStore(this IReliableMessagingConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        if (configurator is not IReliableMessagingProviderConfigurator provider)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Reliable messaging", "unknown", "The Durable Sender configurator does not expose the provider registration contract.", "Correct the named configuration before starting the host"));

        provider.UseStore(typeof(InMemoryReliableStore<>).MakeGenericType(provider.BusType));
        typeof(InMemoryReliableInboxRegistration)
            .GetMethod(nameof(InMemoryReliableInboxRegistration.Add))!
            .MakeGenericMethod(provider.BusType)
            .Invoke(null, [provider.Services]);
        return configurator;
    }

    /// <summary>Registers the in-memory durable-send dispatcher for a bus contract.</summary>
    /// <typeparam name="TBus">The bus contract.</typeparam>
    /// <param name="services">The dependency-injection service collection.</param>
    /// <returns>The same service collection for fluent composition.</returns>
    public static IServiceCollection AddViciOneInMemoryDurableSendDispatcher<TBus>(this IServiceCollection services)
        where TBus : class, IBus
    {
        ArgumentNullException.ThrowIfNull(services);

        if (services.Any(static descriptor => descriptor.ServiceType == typeof(IDurableSendDispatcher<TBus>)))
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Reliable messaging", "unknown", $"A durable-send dispatcher is already registered for bus '{typeof(TBus)}'. Exactly one provider adapter is allowed.", "Correct the named configuration before starting the host"));
        }

        services.AddSingleton<IDurableSendDispatcher<TBus>, InMemoryDurableSendDispatcher<TBus>>();
        ViciOne.ServiceBus.Configuration.BusCompositionRegistrations.AddFeature<TBus>(services, "Durable sender dispatcher");
        return services;
    }
}
