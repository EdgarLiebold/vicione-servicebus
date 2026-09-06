using System;
using System.Linq;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.InMemoryTransport;
using ViciOne.ServiceBus.Providers.Persistence;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Registers the volatile InMemory adapter for one typed durable sender.</summary>
public static class InMemoryDurableSendServiceCollectionExtensions
{
    /// <summary>Selects the volatile in-memory store for tests and local process-only hosts. It does not provide restart durability.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <returns>The configured in memory store.</returns>
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

    /// <summary>Low-level adapter registration. InMemory bus configuration adds this capability automatically.</summary>
    /// <typeparam name="TBus">The bus type.</typeparam>
    /// <param name="services">The dependency-injection service collection.</param>
    /// <returns>The service collection produced by the operation.</returns>
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
