using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.EntityFrameworkCore;
using ViciOne.ServiceBus.Providers.Persistence;

#nullable enable

namespace Microsoft.Extensions.DependencyInjection;
/// <summary>DI registration for the EF Core durable-send persistence provider.</summary>
public static class EntityFrameworkDurableSendServiceCollectionExtensions
{
    /// <summary>Selects EF Core persistence inside the owning bus's Durable Sender configuration.</summary>
    public static IDurableSenderConfigurator UseEntityFramework<TDbContext>(
        this IDurableSenderConfigurator configurator)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(configurator);
        if (configurator is not IDurableSenderProviderConfigurator provider)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Reliable messaging", "unknown", "The Durable Sender configurator does not expose the provider registration contract.", "Correct the named configuration before starting the host"));

        Type validatorService = typeof(IEntityFrameworkDurableSendCommitDurabilityValidator<>).MakeGenericType(provider.BusType);
        Type validatorImplementation = typeof(EntityFrameworkDurableSendCommitDurabilityValidator<>).MakeGenericType(provider.BusType);
        provider.Services.TryAdd(ServiceDescriptor.Singleton(validatorService, validatorImplementation));
        provider.UseStore(typeof(EntityFrameworkDurableSendStore<,>).MakeGenericType(provider.BusType, typeof(TDbContext)));
        return configurator;
    }

    /// <summary>Low-level provider registration. Applications should use UseDurableSender(...UseEntityFramework...).</summary>
    public static IServiceCollection AddEntityFrameworkDurableSendStore<TBus, TDbContext>(
        this IServiceCollection services)
        where TBus : class, IBus
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        if (services.Any(static descriptor => descriptor.ServiceType == typeof(IDurableSendStore<TBus>)))
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Reliable messaging", "unknown", $"A durable-send store is already registered for bus '{typeof(TBus)}'. Exactly one store owner is allowed per typed durable sender.", "Correct the named configuration before starting the host"));

        services.TryAddSingleton<IEntityFrameworkDurableSendCommitDurabilityValidator<TBus>,
            EntityFrameworkDurableSendCommitDurabilityValidator<TBus>>();
        services.AddSingleton<IDurableSendStore<TBus>, EntityFrameworkDurableSendStore<TBus, TDbContext>>();
        return services;
    }
}
