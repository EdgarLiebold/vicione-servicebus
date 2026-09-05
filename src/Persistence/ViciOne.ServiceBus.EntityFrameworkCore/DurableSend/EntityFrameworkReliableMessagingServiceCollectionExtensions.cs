using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.EntityFrameworkCore;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.Outbox;
using ViciOne.ServiceBus.Providers.Persistence;

#nullable enable

namespace Microsoft.Extensions.DependencyInjection;
/// <summary>DI registration for the EF Core reliable-messaging persistence provider.</summary>
public static class EntityFrameworkReliableMessagingServiceCollectionExtensions
{
    /// <summary>Selects EF Core persistence inside the owning bus's reliable-messaging configuration.</summary>
    public static IReliableMessagingConfigurator UseEntityFramework<TDbContext>(
        this IReliableMessagingConfigurator configurator)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(configurator);
        if (configurator is not IReliableMessagingProviderConfigurator provider)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Reliable messaging", "unknown", "The configurator does not expose the provider registration contract.", "Configure the provider inside UseReliableMessaging"));

        Type validatorService = typeof(IEntityFrameworkDurableSendCommitDurabilityValidator<>).MakeGenericType(provider.BusType);
        Type validatorImplementation = typeof(EntityFrameworkDurableSendCommitDurabilityValidator<>).MakeGenericType(provider.BusType);
        provider.Services.TryAdd(ServiceDescriptor.Singleton(validatorService, validatorImplementation));
        provider.UseStore(typeof(EntityFrameworkReliableStore<,>).MakeGenericType(provider.BusType, typeof(TDbContext)));
        RegisterTransactionalSession<TDbContext>(provider);
        return configurator;
    }

    /// <summary>Low-level provider registration. Applications should use UseReliableMessaging(...UseEntityFramework...).</summary>
    public static IServiceCollection AddEntityFrameworkReliableStore<TBus, TDbContext>(
        this IServiceCollection services)
        where TBus : class, IBus
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        if (services.Any(static descriptor => descriptor.ServiceType == typeof(IOutboxStore<TBus>)
                || descriptor.ServiceType == typeof(IInboxStore<TBus>)
                || descriptor.ServiceType == typeof(IScheduleStore<TBus>)))
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Reliable messaging", "unknown", $"A store is already registered for bus '{typeof(TBus)}'.", "Select exactly one reliable store"));

        services.TryAddSingleton<IEntityFrameworkDurableSendCommitDurabilityValidator<TBus>,
            EntityFrameworkDurableSendCommitDurabilityValidator<TBus>>();
        services.AddSingleton<EntityFrameworkReliableStore<TBus, TDbContext>>();
        services.AddSingleton<IOutboxStore<TBus>>(provider => provider.GetRequiredService<EntityFrameworkReliableStore<TBus, TDbContext>>());
        services.AddSingleton<IInboxStore<TBus>>(provider => provider.GetRequiredService<EntityFrameworkReliableStore<TBus, TDbContext>>());
        services.AddSingleton<IScheduleStore<TBus>>(provider => provider.GetRequiredService<EntityFrameworkReliableStore<TBus, TDbContext>>());
        return services;
    }

    static void RegisterTransactionalSession<TDbContext>(IReliableMessagingProviderConfigurator provider)
        where TDbContext : DbContext
    {
        Type helperType = typeof(EntityFrameworkReliableMessagingServiceCollectionExtensions);
        var method = helperType.GetMethod(
            nameof(RegisterTransactionalSessionCore),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .MakeGenericMethod(provider.BusType, typeof(TDbContext));
        method.Invoke(null, [provider.Services]);
    }

    static void RegisterTransactionalSessionCore<TBus, TDbContext>(IServiceCollection services)
        where TBus : class, IBus
        where TDbContext : DbContext
    {
        services.TryAddScoped<EntityFrameworkBusOutboxSessionRegistry<TBus>>();
        services.AddScoped<EntityFrameworkScopedBusContext<TBus, TDbContext>>(provider =>
            provider.GetRequiredService<EntityFrameworkBusOutboxSessionRegistry<TBus>>().GetOrCreate<TDbContext>(provider));
        services.AddSingleton<IEntityFrameworkScopedBusContextFactory<TBus>>(
            new EntityFrameworkScopedBusContextFactory<TBus, TDbContext>(isDefault: false));
        services.ReplaceScoped<IScopedBusContextProvider<TBus>, EntityFrameworkScopedBusContextProvider<TBus>>();
        services.AddScoped<IEntityFrameworkTransactionalOutbox<TBus, TDbContext>>(provider =>
            provider.GetRequiredService<EntityFrameworkScopedBusContext<TBus, TDbContext>>());
        services.AddSingleton<IBusOutboxNotification<EntityFrameworkBusOutboxScope<TBus, TDbContext>>,
            BusOutboxNotification<EntityFrameworkBusOutboxScope<TBus, TDbContext>>>();
        services.AddScoped<IOutboxContextFactory<EntityFrameworkReliableInboxScope<TBus, TDbContext>>,
            EntityFrameworkReliableInboxContextFactory<TBus, TDbContext>>();
        services.AddSingleton(provider => Bind<TBus>.Create<IConfigureReceiveEndpoint>(
            new EntityFrameworkReliableInboxEndpointConfiguration<TBus, TDbContext>(
                provider.GetRequiredService<Bind<TBus, IBusRegistrationContext>>().Value,
                provider.GetRequiredService<IOptions<ReliableMessagingOptions<TBus>>>().Value.MaximumDeliveryAttempts)));
    }
}
