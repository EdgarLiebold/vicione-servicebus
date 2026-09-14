using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.DependencyInjection;

namespace ViciOne.ServiceBus.Configuration;

// Separates rider-owned registrations from registrations owned directly by the same bus.
sealed class RiderRegistrationOwner
{
}


/// <summary>Stores rider registrations in a bus-specific dependency-injection partition.</summary>
/// <typeparam name="TBus">The bus contract that owns the rider.</typeparam>
internal sealed class DependencyInjectionRiderContainerRegistrar<TBus> :
    DependencyInjectionContainerRegistrar
{
    /// <summary>Creates a registrar over the supplied service collection.</summary>
    /// <param name="collection">The service collection that owns rider registrations.</param>
    public DependencyInjectionRiderContainerRegistrar(IServiceCollection collection)
        : base(collection)
    {
    }

    /// <summary>Returns rider registrations available before the container is built.</summary>
    /// <typeparam name="T">The registration category.</typeparam>
    /// <returns>The registrations owned by this bus rider.</returns>
    public override IEnumerable<T> GetRegistrations<T>()
    {
        return Collection.Where(x => x.ServiceType == typeof(Bind<TBus, RiderRegistrationOwner, T>))
            .Select(x => x.ImplementationInstance).OfType<Bind<TBus, RiderRegistrationOwner, T>>()
            .Select(x => x.Value);
    }

    /// <summary>Resolves rider registrations from a built container.</summary>
    /// <typeparam name="T">The registration category.</typeparam>
    /// <param name="provider">The service provider that contains the registrations.</param>
    /// <returns>The registrations owned by this bus rider.</returns>
    public override IEnumerable<T> GetRegistrations<T>(IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        return (provider.GetService<IEnumerable<Bind<TBus, RiderRegistrationOwner, T>>>() ?? []).Select(x => x.Value);
    }

    /// <summary>Adds a registration to the rider's bus-specific partition.</summary>
    /// <typeparam name="T">The registration category.</typeparam>
    /// <param name="value">The registration to add.</param>
    protected override void AddRegistration<T>(T value)
    {
        ArgumentNullException.ThrowIfNull(value);

        Collection.Add(ServiceDescriptor.Singleton(Bind<TBus, RiderRegistrationOwner>.Create(value)));
    }
}
