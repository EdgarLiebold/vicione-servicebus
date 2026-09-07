using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.DependencyInjection;

namespace ViciOne.ServiceBus.EntityFrameworkCore;
/// <summary>
/// Selects the EF bus outbox for a scoped bus. Selection is deterministic: one registration is implicit, multiple
/// registrations require exactly one explicit default. DbContext-specific APIs bypass this selector entirely.
/// </summary>
/// <typeparam name="TBus">The bus type.</typeparam>
internal sealed class EntityFrameworkScopedBusContextProvider<TBus> : IScopedBusContextProvider<TBus>
    where TBus : class, IBus
{
    public EntityFrameworkScopedBusContextProvider(IEnumerable<IEntityFrameworkScopedBusContextFactory<TBus>> factories, IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(factories);
        ArgumentNullException.ThrowIfNull(provider);

        var registrations = factories.ToArray();
        if (registrations.Length == 0)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Entity Framework Scoped Bus Context Provider", "unknown", $"No Entity Framework bus outbox is registered for {TypeCache<TBus>.ShortName}.", "Correct the named configuration before starting the host"));

        IEntityFrameworkScopedBusContextFactory<TBus> selected;
        if (registrations.Length == 1)
            selected = registrations[0];
        else
        {
            var defaults = registrations.Where(x => x.IsDefault).ToArray();
            selected = defaults.Length switch
            {
                1 => defaults[0],
                0 => throw new ConfigurationException(
                    global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Entity Framework Scoped Bus Context Provider", "unknown", $"Multiple Entity Framework bus outboxes are configured for {TypeCache<TBus>.ShortName}. "
                    + "An explicit default DbContext is required for untyped scoped publish/send endpoints.", "Correct the named configuration before starting the host")),
                _ => throw new ConfigurationException(
                    global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Entity Framework Scoped Bus Context Provider", "unknown", $"Multiple default Entity Framework bus outboxes are configured for {TypeCache<TBus>.ShortName}. Exactly one default is allowed.", "Correct the named configuration before starting the host"))
            };
        }

        Context = selected.Create(provider);
    }

    public ScopedBusContext Context { get; }
}
