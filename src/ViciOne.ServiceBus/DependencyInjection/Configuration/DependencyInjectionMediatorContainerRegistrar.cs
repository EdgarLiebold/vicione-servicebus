using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Clients;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Mediator;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Registers dependency injection mediator container components with dependency injection.</summary>
public class DependencyInjectionMediatorContainerRegistrar :
    DependencyInjectionContainerRegistrar
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="collection">The collection.</param>
    public DependencyInjectionMediatorContainerRegistrar(IServiceCollection collection)
        : base(collection)
    {
    }

    /// <summary>Gets registrations.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The registrations.</returns>
    public override IEnumerable<T> GetRegistrations<T>()
    {
        return Collection.Where(x => x.ServiceType == typeof(Bind<IMediator, T>))
            .Select(x => x.ImplementationInstance).Cast<Bind<IMediator, T>>()
            .Select(x => x.Value);
    }

    /// <summary>Gets registrations.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    /// <returns>The registrations.</returns>
    public override IEnumerable<T> GetRegistrations<T>(IServiceProvider provider)
    {
        return (provider.GetService<IEnumerable<Bind<IMediator, T>>>() ?? []).Select(x => x.Value);
    }

    /// <summary>Adds registration to the configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value to process.</param>
    protected override void AddRegistration<T>(T value)
    {
        Collection.Add(ServiceDescriptor.Singleton(Bind<IMediator>.Create(value)));
    }

    /// <summary>Gets scoped bus context.</summary>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    /// <returns>The scoped bus context.</returns>
    protected override IScopedClientFactory GetScopedBusContext(IServiceProvider provider)
    {
        var clientFactory = provider.GetRequiredService<IScopedMediator>();
        var consumeContextProvider = provider.GetRequiredService<Bind<IMediator, IScopedConsumeContextProvider>>().Value;

        return consumeContextProvider.HasContext
            ? new ScopedClientFactory(clientFactory, consumeContextProvider.GetContext())
            : new ScopedClientFactory(new ClientFactory(new ScopedClientFactoryContext(clientFactory, provider)), null);
    }
}
