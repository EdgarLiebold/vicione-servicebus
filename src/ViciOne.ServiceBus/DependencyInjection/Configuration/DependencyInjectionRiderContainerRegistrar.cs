using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.DependencyInjection;

namespace ViciOne.ServiceBus.Configuration;

abstract class Rider
{
}


/// <summary>Registers dependency injection rider container components with dependency injection.</summary>
/// <typeparam name="TBus">The bus type.</typeparam>
public class DependencyInjectionRiderContainerRegistrar<TBus> :
    DependencyInjectionContainerRegistrar
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="collection">The collection.</param>
    public DependencyInjectionRiderContainerRegistrar(IServiceCollection collection)
        : base(collection)
    {
    }

    /// <summary>Gets registrations.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The registrations.</returns>
    public override IEnumerable<T> GetRegistrations<T>()
    {
        return Collection.Where(x => x.ServiceType == typeof(Bind<TBus, Rider, T>))
            .Select(x => x.ImplementationInstance).Cast<Bind<TBus, Rider, T>>()
            .Select(x => x.Value);
    }

    /// <summary>Gets registrations.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    /// <returns>The registrations.</returns>
    public override IEnumerable<T> GetRegistrations<T>(IServiceProvider provider)
    {
        return (provider.GetService<IEnumerable<Bind<TBus, Rider, T>>>() ?? []).Select(x => x.Value);
    }

    /// <summary>Adds registration to the configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value to process.</param>
    protected override void AddRegistration<T>(T value)
    {
        Collection.Add(ServiceDescriptor.Singleton(Bind<TBus, Rider>.Create(value)));
    }
}
