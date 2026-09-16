using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Clients;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Middleware.Outbox;
using ViciOne.ServiceBus.Providers.Persistence;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

internal sealed class EntityFrameworkScopedBusContextFactory<TBus, TDbContext> :
    IEntityFrameworkScopedBusContextFactory<TBus>
    where TBus : class, IBus
    where TDbContext : DbContext
{
    public EntityFrameworkScopedBusContextFactory(bool isDefault)
    {
        IsDefault = isDefault;
    }

    public Type DbContextType => typeof(TDbContext);
    public bool IsDefault { get; }

    public ScopedBusContext Create(IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        var clientFactory = provider.GetRequiredService<Bind<TBus, IClientFactory>>().Value;
        var consumeContextProvider = provider.GetRequiredService<Bind<TBus, IScopedConsumeContextProvider>>().Value;

        // Sends performed while consuming on this same bus belong to the receive pipeline/outbox.
        if (consumeContextProvider.TryGetContext(out var context))
            return new ConsumeContextScopedBusContext(context, clientFactory);

        return provider.GetRequiredService<EntityFrameworkScopedBusContext<TBus, TDbContext>>();
    }

    internal static EntityFrameworkScopedBusContext<TBus, TDbContext> CreateTransactionalContext(IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        var bus = provider.GetRequiredService<TBus>();
        var dbContext = provider.GetRequiredService<TDbContext>();
        var notification = provider.GetRequiredService<IBusOutboxNotification<EntityFrameworkBusOutboxScope<TBus, TDbContext>>>();
        var clientFactory = provider.GetRequiredService<Bind<TBus, IClientFactory>>().Value;
        var globalConsumeContextProvider = provider.GetRequiredService<IScopedConsumeContextProvider>();
        var timeProvider = provider.GetRequiredService<TimeProvider>();
        var persistenceIdentity = provider.GetRequiredService<BusPersistenceIdentity<TBus>>();

        if (globalConsumeContextProvider.TryGetContext(out var context))
        {
            return new EntityFrameworkConsumeContextScopedBusContext<TBus, TDbContext>(bus, dbContext, notification, clientFactory, provider,
                context, timeProvider, persistenceIdentity);
        }

        return new EntityFrameworkScopedBusContext<TBus, TDbContext>(
            bus,
            dbContext,
            notification,
            clientFactory,
            provider,
            timeProvider,
            persistenceIdentity);
    }
}
