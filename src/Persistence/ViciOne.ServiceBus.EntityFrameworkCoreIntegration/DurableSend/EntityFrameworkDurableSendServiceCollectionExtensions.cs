#nullable enable

namespace Microsoft.Extensions.DependencyInjection;

using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.EntityFrameworkCoreIntegration;

/// <summary>DI registration for the EF Core durable-send persistence provider.</summary>
public static class EntityFrameworkDurableSendServiceCollectionExtensions
{
    public static IServiceCollection AddEntityFrameworkDurableSendStore<TBus, TDbContext>(
        this IServiceCollection services,
        Action<EntityFrameworkDurableSendStoreOptions<TBus>> configure)
        where TBus : class, IBus
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        if (services.Any(static descriptor => descriptor.ServiceType == typeof(IDurableSendStore<TBus>)))
            throw new ConfigurationException(
                $"A durable-send store is already registered for bus '{typeof(TBus)}'. Exactly one store owner is allowed per typed durable sender.");

        services.AddOptions<EntityFrameworkDurableSendStoreOptions<TBus>>().Configure(configure);
        services.TryAddSingleton<IEntityFrameworkDurableSendCommitDurabilityValidator<TBus>,
            EntityFrameworkDurableSendCommitDurabilityValidator<TBus>>();
        services.AddSingleton<IDurableSendStore<TBus>, EntityFrameworkDurableSendStore<TBus, TDbContext>>();
        return services;
    }
}
