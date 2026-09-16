using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Clients;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Middleware.Outbox;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

internal sealed class EntityFrameworkTransactionalScopedBusContextFactory<TBus, TDbContext> :
    IEntityFrameworkScopedBusContextFactory<TBus>
    where TBus : class, IBus
    where TDbContext : DbContext
{
    public EntityFrameworkTransactionalScopedBusContextFactory(bool isDefault)
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
        if (consumeContextProvider.TryGetContext(out var context))
            return new ConsumeContextScopedBusContext(context, clientFactory);

        return provider.GetRequiredService<EntityFrameworkTransactionalScopedBusContext<TBus, TDbContext>>();
    }

    internal static EntityFrameworkTransactionalScopedBusContext<TBus, TDbContext> CreateTransactionalContext(
        IServiceProvider provider)
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
            return new EntityFrameworkTransactionalConsumeContextScopedBusContext<TBus, TDbContext>(
                bus,
                dbContext,
                notification,
                clientFactory,
                provider,
                context,
                timeProvider,
                persistenceIdentity);
        }

        return new EntityFrameworkTransactionalScopedBusContext<TBus, TDbContext>(
            bus,
            dbContext,
            notification,
            clientFactory,
            provider,
            timeProvider,
            persistenceIdentity);
    }
}
