using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Clients;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Mediator;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a dependency injection mediator container registrar implementation.
/// </summary>
public class DependencyInjectionMediatorContainerRegistrar :
    DependencyInjectionContainerRegistrar
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="collection">The collection value.</param>
    public DependencyInjectionMediatorContainerRegistrar(IServiceCollection collection)
        : base(collection)
    {
    }

    /// <summary>
    /// Gets registrations.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public override IEnumerable<T> GetRegistrations<T>()
    {
        return Collection.Where(x => x.ServiceType == typeof(Bind<IMediator, T>))
            .Select(x => x.ImplementationInstance).Cast<Bind<IMediator, T>>()
            .Select(x => x.Value);
    }

    /// <summary>
    /// Gets registrations.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="provider">The service provider.</param>
    /// <returns>The result of the operation.</returns>
    public override IEnumerable<T> GetRegistrations<T>(IServiceProvider provider)
    {
        return (provider.GetService<IEnumerable<Bind<IMediator, T>>>() ?? []).Select(x => x.Value);
    }

    /// <summary>
    /// Adds registration to the configuration.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="value">The value.</param>
    protected override void AddRegistration<T>(T value)
    {
        Collection.Add(ServiceDescriptor.Singleton(Bind<IMediator>.Create(value)));
    }

    /// <summary>
    /// Gets scoped bus context.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    /// <returns>The result of the operation.</returns>
    protected override IScopedClientFactory GetScopedBusContext(IServiceProvider provider)
    {
        var clientFactory = provider.GetRequiredService<IScopedMediator>();
        var consumeContextProvider = provider.GetRequiredService<Bind<IMediator, IScopedConsumeContextProvider>>().Value;

        return consumeContextProvider.HasContext
            ? new ScopedClientFactory(clientFactory, consumeContextProvider.GetContext())
            : new ScopedClientFactory(new ClientFactory(new ScopedClientFactoryContext(clientFactory, provider)), null);
    }
}
