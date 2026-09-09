using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using ViciOne.ServiceBus.DependencyInjection;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>
/// Caches exactly one transactional EF bus-outbox session per DbContext type for one bus and DI scope.
/// Each cached session is also resolved through its own scoped DI descriptor, which owns disposal before the DbContext.
/// </summary>
/// <typeparam name="TBus">The bus type.</typeparam>
internal sealed class EntityFrameworkBusOutboxSessionRegistry<TBus>
    where TBus : class, IBus
{
    readonly object _sync = new();
    readonly Dictionary<(Type DbContextType, bool Reliable), ScopedBusContext> _sessions = new();

    public EntityFrameworkScopedBusContext<TBus, TDbContext> GetOrCreate<TDbContext>(IServiceProvider provider)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(provider);

        lock (_sync)
        {
            var key = (typeof(TDbContext), Reliable: true);
            if (_sessions.TryGetValue(key, out var existing))
                return (EntityFrameworkScopedBusContext<TBus, TDbContext>)existing;

            var created = EntityFrameworkScopedBusContextFactory<TBus, TDbContext>.CreateTransactionalContext(provider);
            _sessions.Add(key, created);
            return created;
        }
    }

    public EntityFrameworkTransactionalScopedBusContext<TBus, TDbContext> GetOrCreateTransactional<TDbContext>(IServiceProvider provider)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(provider);

        lock (_sync)
        {
            var key = (typeof(TDbContext), Reliable: false);
            if (_sessions.TryGetValue(key, out var existing))
                return (EntityFrameworkTransactionalScopedBusContext<TBus, TDbContext>)existing;

            var created = EntityFrameworkTransactionalScopedBusContextFactory<TBus, TDbContext>
                .CreateTransactionalContext(provider);
            _sessions.Add(key, created);
            return created;
        }
    }
}
