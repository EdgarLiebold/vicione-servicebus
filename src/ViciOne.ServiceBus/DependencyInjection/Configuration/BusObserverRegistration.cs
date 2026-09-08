using System;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Associates an observer implementation with one bus registration.</summary>
internal sealed class BusObserverRegistration<TBus, TObserver> : IBusObserverRegistration
    where TBus : class, IBus
    where TObserver : class, IBusObserver
{
    /// <summary>Gets the stable key of the owning bus registration.</summary>
    public string BusKey { get; } = BusRegistrationIdentity.GetKey(typeof(TBus));

    /// <summary>Resolves the observer registered for the owning bus.</summary>
    /// <param name="provider">The provider that owns the bus registration.</param>
    /// <returns>The bus-specific observer.</returns>
    public IBusObserver Resolve(IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        return provider.GetRequiredService<TObserver>();
    }
}
