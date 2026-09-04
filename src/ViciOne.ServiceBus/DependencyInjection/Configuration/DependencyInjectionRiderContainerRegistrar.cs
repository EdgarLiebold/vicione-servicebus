using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.DependencyInjection;

namespace ViciOne.ServiceBus.Configuration;

abstract class Rider
{
}


public class DependencyInjectionRiderContainerRegistrar<TBus> :
    DependencyInjectionContainerRegistrar
{
    public DependencyInjectionRiderContainerRegistrar(IServiceCollection collection)
        : base(collection)
    {
    }

    public override IEnumerable<T> GetRegistrations<T>()
    {
        return Collection.Where(x => x.ServiceType == typeof(Bind<TBus, Rider, T>))
            .Select(x => x.ImplementationInstance).Cast<Bind<TBus, Rider, T>>()
            .Select(x => x.Value);
    }

    public override IEnumerable<T> GetRegistrations<T>(IServiceProvider provider)
    {
        return provider.GetService<IEnumerable<Bind<TBus, Rider, T>>>().Select(x => x.Value) ?? [];
    }

    protected override void AddRegistration<T>(T value)
    {
        Collection.Add(ServiceDescriptor.Singleton(Bind<TBus, Rider>.Create(value)));
    }
}
