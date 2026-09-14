using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Registers a rider and its services for the default bus.</summary>
internal class ServiceCollectionRiderConfigurator :
    RegistrationConfigurator,
    IRiderRegistrationConfigurator
{
    /// <summary>Creates a rider configurator over a service collection.</summary>
    /// <param name="collection">The service collection that owns the rider.</param>
    /// <param name="registrar">The container registrar that isolates rider registrations.</param>
    public ServiceCollectionRiderConfigurator(IServiceCollection collection, IContainerRegistrar registrar)
        : base(collection, registrar)
    {
    }

    /// <summary>Adds a scoped rider service unless its service type is already registered.</summary>
    /// <typeparam name="TRider">The rider runtime contract.</typeparam>
    /// <typeparam name="TService">The scoped service to expose.</typeparam>
    /// <param name="factory">The factory that receives the rider and current service provider.</param>
    public virtual void TryAddScoped<TRider, TService>(Func<TRider, IServiceProvider, TService> factory)
        where TRider : class, IRider
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(factory);

        Services.TryAddScoped(provider => factory(provider.GetRequiredService<Bind<IBus, TRider>>().Value, provider));
    }

    /// <summary>Registers the factory and bus-scoped services for a rider runtime.</summary>
    /// <typeparam name="TRider">The rider runtime contract.</typeparam>
    /// <param name="riderFactory">The factory that creates the rider runtime.</param>
    public virtual void SetRiderFactory<TRider>(IRegistrationRiderFactory<TRider> riderFactory)
        where TRider : class, IRider
    {
        if (riderFactory == null)
            throw new ArgumentNullException(nameof(riderFactory));

        ThrowIfAlreadyConfigured(typeof(TRider));

        IRiderRegistrationContext CreateRegistrationContext(IServiceProvider provider)
        {
            var setter = provider.GetRequiredService<Bind<IBus, TRider, ISetScopedConsumeContext>>();
            var registration = CreateRegistration(provider, setter.Value);
            return new RiderRegistrationContext(registration, Registrar);
        }

        static Bind<IBus, TRider, IScopedConsumeContextProvider> CreateScopeProvider(IServiceProvider provider)
        {
            var global = provider.GetRequiredService<IScopedConsumeContextProvider>();
            return Bind<IBus, TRider>.Create((IScopedConsumeContextProvider)new TypedScopedConsumeContextProvider(global));
        }

        Services.TryAddScoped(CreateScopeProvider);

        Services.AddSingleton(_ => Bind<IBus, TRider>.Create((ISetScopedConsumeContext)new SetScopedConsumeContext<IBus>(provider =>
            provider.GetRequiredService<Bind<IBus, TRider, IScopedConsumeContextProvider>>().Value)));
        Services.AddSingleton(provider => Bind<IBus, TRider>.Create(CreateRegistrationContext(provider)));
        Services.AddSingleton(provider => Bind<IBus>.Create(
            riderFactory.CreateRider(provider.GetRequiredService<Bind<IBus, TRider, IRiderRegistrationContext>>().Value)
            ?? throw new InvalidOperationException("The rider factory returned null.")));
        Services.AddSingleton(provider => Bind<IBus>.Create(provider.GetRequiredService<Bind<IBus, IBusInstance>>().Value.GetRider<TRider>()));
        Services.AddSingleton(provider => provider.GetRequiredService<Bind<IBus, TRider>>().Value);
    }

    /// <summary>Rejects a second rider factory or a duplicate rider service.</summary>
    /// <param name="serviceType">The service type that uniquely identifies the rider.</param>
    protected void ThrowIfAlreadyConfigured(Type serviceType)
    {
        ThrowIfAlreadyConfigured(nameof(SetRiderFactory));
        if (Services.Any(d => d.ServiceType == serviceType))
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Service Collection Rider", "unknown", $"'{serviceType.Name}' has been already registered.", "Correct the named configuration before starting the host"));
    }
}


/// <summary>Registers a rider and its services for a typed bus.</summary>
/// <typeparam name="TBus">The application-facing bus contract.</typeparam>
internal sealed class ServiceCollectionRiderConfigurator<TBus> :
    ServiceCollectionRiderConfigurator,
    IRiderRegistrationConfigurator<TBus>
    where TBus : class, IBus
{
    /// <summary>Creates a typed-bus rider configurator over a service collection.</summary>
    /// <param name="collection">The service collection that owns the rider.</param>
    /// <param name="registrar">The container registrar that isolates rider registrations.</param>
    public ServiceCollectionRiderConfigurator(IServiceCollection collection, IContainerRegistrar registrar)
        : base(collection, registrar)
    {
    }

    /// <summary>Adds a typed-bus scoped rider service unless its service type is already registered.</summary>
    /// <typeparam name="TRider">The rider runtime contract.</typeparam>
    /// <typeparam name="TService">The scoped service to expose.</typeparam>
    /// <param name="factory">The factory that receives the rider and current service provider.</param>
    public override void TryAddScoped<TRider, TService>(Func<TRider, IServiceProvider, TService> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        Services.TryAddScoped(provider => factory(provider.GetRequiredService<Bind<TBus, TRider>>().Value, provider));
    }

    /// <summary>Registers the factory and typed-bus services for a rider runtime.</summary>
    /// <typeparam name="TRider">The rider runtime contract.</typeparam>
    /// <param name="riderFactory">The factory that creates the rider runtime.</param>
    public override void SetRiderFactory<TRider>(IRegistrationRiderFactory<TRider> riderFactory)
    {
        if (riderFactory == null)
            throw new ArgumentNullException(nameof(riderFactory));

        ThrowIfAlreadyConfigured(typeof(Bind<TBus, TRider>));

        IRiderRegistrationContext CreateRegistrationContext(IServiceProvider provider)
        {
            var setter = provider.GetRequiredService<Bind<TBus, TRider, ISetScopedConsumeContext>>();
            var registration = CreateRegistration(provider, setter.Value);
            return new RiderRegistrationContext(registration, Registrar);
        }

        static Bind<TBus, TRider, IScopedConsumeContextProvider> CreateScopeProvider(IServiceProvider provider)
        {
            var global = provider.GetRequiredService<IScopedConsumeContextProvider>();
            return Bind<TBus, TRider>.Create((IScopedConsumeContextProvider)new TypedScopedConsumeContextProvider(global));
        }

        Services.TryAddScoped(CreateScopeProvider);

        Services.AddSingleton(_ => Bind<TBus, TRider>.Create((ISetScopedConsumeContext)new SetScopedConsumeContext<TBus>(provider =>
            provider.GetRequiredService<Bind<TBus, TRider, IScopedConsumeContextProvider>>().Value)));
        Services.AddSingleton(provider => Bind<TBus, TRider>.Create(CreateRegistrationContext(provider)));
        Services.AddSingleton(provider => Bind<TBus>.Create(
            riderFactory.CreateRider(provider.GetRequiredService<Bind<TBus, TRider, IRiderRegistrationContext>>().Value)
            ?? throw new InvalidOperationException("The rider factory returned null.")));
        Services.AddSingleton(provider => Bind<TBus>.Create(provider.GetRequiredService<IBusInstance<TBus>>().GetRider<TRider>()));
    }
}
