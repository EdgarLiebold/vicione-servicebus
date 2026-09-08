using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Resolves an observer that belongs to one identified bus registration.</summary>
internal interface IBusObserverRegistration
{
    /// <summary>Gets the stable key of the owning bus registration.</summary>
    string BusKey { get; }

    /// <summary>Resolves the observer from the active service provider.</summary>
    /// <param name="provider">The provider that owns the bus registration.</param>
    /// <returns>The observer associated with the owning bus.</returns>
    IBusObserver Resolve(IServiceProvider provider);
}
