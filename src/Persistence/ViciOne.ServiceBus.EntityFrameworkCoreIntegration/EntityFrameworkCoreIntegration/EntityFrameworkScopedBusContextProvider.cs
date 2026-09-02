#nullable enable
namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration;

using System;
using System.Collections.Generic;
using System.Linq;
using DependencyInjection;


/// <summary>
/// Selects the EF bus outbox for a scoped bus. Selection is deterministic: one registration is implicit, multiple
/// registrations require exactly one explicit default. DbContext-specific APIs bypass this selector entirely.
/// </summary>
internal sealed class EntityFrameworkScopedBusContextProvider<TBus> : IScopedBusContextProvider<TBus>
    where TBus : class, IBus
{
    public EntityFrameworkScopedBusContextProvider(IEnumerable<IEntityFrameworkScopedBusContextFactory<TBus>> factories, IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(factories);
        ArgumentNullException.ThrowIfNull(provider);

        var registrations = factories.ToArray();
        if (registrations.Length == 0)
            throw new ConfigurationException($"No Entity Framework bus outbox is registered for {TypeCache<TBus>.ShortName}.");

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
                    $"Multiple Entity Framework bus outboxes are configured for {TypeCache<TBus>.ShortName}. "
                    + "An explicit default DbContext is required for untyped scoped publish/send endpoints."),
                _ => throw new ConfigurationException(
                    $"Multiple default Entity Framework bus outboxes are configured for {TypeCache<TBus>.ShortName}. Exactly one default is allowed.")
            };
        }

        Context = selected.Create(provider);
    }

    public ScopedBusContext Context { get; }
}
