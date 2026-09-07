using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Clients;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Mediator;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Stores mediator registrations and creates scoped client factories from the service provider.</summary>
internal sealed class MediatorContainerRegistrar :
    DependencyInjectionContainerRegistrar
{
    /// <summary>Initializes a registrar backed by the mediator service collection.</summary>
    /// <param name="collection">The service collection that owns mediator registrations.</param>
    public MediatorContainerRegistrar(IServiceCollection collection)
        : base(collection)
    {
    }

    /// <summary>Returns registrations already materialized in the service collection.</summary>
    /// <typeparam name="T">The registered mediator component type.</typeparam>
    /// <returns>The matching registered components.</returns>
    public override IEnumerable<T> GetRegistrations<T>()
    {
        return Collection.Where(x => x.ServiceType == typeof(Bind<IMediator, T>))
            .Select(x => x.ImplementationInstance).Cast<Bind<IMediator, T>>()
            .Select(x => x.Value);
    }

    /// <summary>Resolves registrations from a built service provider.</summary>
    /// <typeparam name="T">The registered mediator component type.</typeparam>
    /// <param name="provider">The service provider that resolves registrations.</param>
    /// <returns>The matching registered components.</returns>
    public override IEnumerable<T> GetRegistrations<T>(IServiceProvider provider)
    {
        return (provider.GetService<IEnumerable<Bind<IMediator, T>>>() ?? []).Select(x => x.Value);
    }

    /// <summary>Adds a mediator-owned singleton registration.</summary>
    /// <typeparam name="T">The registered mediator component type.</typeparam>
    /// <param name="value">The component instance to register.</param>
    protected override void AddRegistration<T>(T value)
    {
        Collection.Add(ServiceDescriptor.Singleton(Bind<IMediator>.Create(value)));
    }

    /// <summary>Creates the client factory for the current mediator scope.</summary>
    /// <param name="provider">The current scoped service provider.</param>
    /// <returns>A client factory bound to the current consume context when one exists.</returns>
    protected override IScopedClientFactory GetScopedBusContext(IServiceProvider provider)
    {
        var clientFactory = provider.GetRequiredService<IScopedMediator>();
        var consumeContextProvider = provider.GetRequiredService<Bind<IMediator, IScopedConsumeContextProvider>>().Value;

        return consumeContextProvider.HasContext
            ? new ScopedClientFactory(clientFactory, consumeContextProvider.GetContext())
            : new ScopedClientFactory(new ClientFactory(new ScopedClientFactoryContext(clientFactory, provider)), null);
    }
}
