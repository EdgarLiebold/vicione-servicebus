#nullable enable

namespace ViciOne.ServiceBus.DurableSend;

using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.ProviderAbstractions;

internal sealed class DurableSenderConfigurator<TBus> :
    IDurableSenderConfigurator<TBus>,
    IDurableSenderProviderConfigurator
    where TBus : class, IBus
{
    public DurableSenderConfigurator(IServiceCollection services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public IServiceCollection Services { get; }

    public Type BusType => typeof(TBus);

    public void Configure(Action<DurableSenderOptions<TBus>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        Services.Configure(configure);
    }

    public void AddMessageContract<TMessage>(string name, int majorVersion = 1)
        where TMessage : class
        => Services.AddViciOneMessageContracts(catalog => catalog.Register<TMessage>(name, majorVersion));

    public void AddMessageContract<TMessage>()
        where TMessage : class
        => Services.AddViciOneMessageContracts(catalog => catalog.Register<TMessage>());

    public void UseStore(Type implementationType)
    {
        ArgumentNullException.ThrowIfNull(implementationType);
        EnsureSingleOwner<IDurableSendStore<TBus>>("persistence store");
        Type serviceType = typeof(IDurableSendStore<TBus>);
        if (!serviceType.IsAssignableFrom(implementationType))
            throw new ArgumentException($"'{implementationType}' does not implement '{serviceType}'.", nameof(implementationType));

        Services.AddSingleton(serviceType, implementationType);
    }

    public void UseDispatcher(Type implementationType)
    {
        ArgumentNullException.ThrowIfNull(implementationType);
        EnsureSingleOwner<IDurableSendDispatcher<TBus>>("transport dispatcher");
        Type serviceType = typeof(IDurableSendDispatcher<TBus>);
        if (!serviceType.IsAssignableFrom(implementationType))
            throw new ArgumentException($"'{implementationType}' does not implement '{serviceType}'.", nameof(implementationType));

        Services.AddSingleton(serviceType, implementationType);
    }

    private void EnsureSingleOwner<TService>(string component)
    {
        if (Services.Any(static descriptor => descriptor.ServiceType == typeof(TService)))
        {
            throw new ConfigurationException(
                $"Durable Sender for bus '{typeof(TBus)}' already has a {component}. Exactly one owner is allowed.");
        }
    }
}

internal sealed class DurableSenderRegistration<TBus>
    where TBus : class, IBus;
