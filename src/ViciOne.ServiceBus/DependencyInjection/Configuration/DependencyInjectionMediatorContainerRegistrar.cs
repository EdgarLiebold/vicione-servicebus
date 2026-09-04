using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Clients;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Mediator;

namespace ViciOne.ServiceBus.Configuration;

public class DependencyInjectionMediatorContainerRegistrar :
    DependencyInjectionContainerRegistrar
{
    public DependencyInjectionMediatorContainerRegistrar(IServiceCollection collection)
        : base(collection)
    {
    }

    public override IEnumerable<T> GetRegistrations<T>()
    {
        return Collection.Where(x => x.ServiceType == typeof(Bind<IMediator, T>))
            .Select(x => x.ImplementationInstance).Cast<Bind<IMediator, T>>()
            .Select(x => x.Value);
    }

    public override IEnumerable<T> GetRegistrations<T>(IServiceProvider provider)
    {
        return provider.GetService<IEnumerable<Bind<IMediator, T>>>().Select(x => x.Value) ?? [];
    }

    protected override void AddRegistration<T>(T value)
    {
        Collection.Add(ServiceDescriptor.Singleton(Bind<IMediator>.Create(value)));
    }

    protected override IScopedClientFactory GetScopedBusContext(IServiceProvider provider)
    {
        var clientFactory = provider.GetRequiredService<IScopedMediator>();
        var consumeContextProvider = provider.GetRequiredService<Bind<IMediator, IScopedConsumeContextProvider>>().Value;

        return consumeContextProvider.HasContext
            ? new ScopedClientFactory(clientFactory, consumeContextProvider.GetContext())
            : new ScopedClientFactory(new ClientFactory(new ScopedClientFactoryContext(clientFactory, provider)), null);
    }
}
