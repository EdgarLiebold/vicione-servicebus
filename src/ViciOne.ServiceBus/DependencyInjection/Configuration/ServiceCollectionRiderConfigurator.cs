using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a service collection rider configurator implementation.
/// </summary>
public class ServiceCollectionRiderConfigurator :
    RegistrationConfigurator,
    IRiderRegistrationConfigurator
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="collection">The collection value.</param>
    /// <param name="registrar">The registrar value.</param>
    public ServiceCollectionRiderConfigurator(IServiceCollection collection, IContainerRegistrar registrar)
        : base(collection, registrar)
    {
    }

    /// <summary>
    /// Performs the try add scoped operation.
    /// </summary>
    /// <typeparam name="TRider">The t rider type.</typeparam>
    /// <typeparam name="TService">The t service type.</typeparam>
    /// <param name="factory">The factory value.</param>
    public virtual void TryAddScoped<TRider, TService>(Func<TRider, IServiceProvider, TService> factory)
        where TRider : class, IRider
        where TService : class
    {
        Services.TryAddScoped(provider => factory(provider.GetRequiredService<Bind<IBus, TRider>>().Value, provider));
    }

    /// <summary>
    /// Sets rider factory.
    /// </summary>
    /// <typeparam name="TRider">The t rider type.</typeparam>
    /// <param name="riderFactory">The rider factory value.</param>
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
        Services.AddSingleton(provider =>
            Bind<IBus>.Create(riderFactory.CreateRider(provider.GetRequiredService<Bind<IBus, TRider, IRiderRegistrationContext>>().Value)));
        Services.AddSingleton(provider => Bind<IBus>.Create(provider.GetRequiredService<Bind<IBus, IBusInstance>>().Value.GetRider<TRider>()));
        Services.AddSingleton(provider => provider.GetRequiredService<Bind<IBus, TRider>>().Value);
    }

    /// <summary>
    /// Performs the throw if already configured operation.
    /// </summary>
    /// <param name="serviceType">The service type value.</param>
    protected void ThrowIfAlreadyConfigured(Type serviceType)
    {
        ThrowIfAlreadyConfigured(nameof(SetRiderFactory));
        if (Services.Any(d => d.ServiceType == serviceType))
            throw new ConfigurationException($"'{serviceType.Name}' has been already registered.");
    }
}


/// <summary>
/// Provides a service collection rider configurator implementation.
/// </summary>
/// <typeparam name="TBus">The t bus type.</typeparam>
public class ServiceCollectionRiderConfigurator<TBus> :
    ServiceCollectionRiderConfigurator,
    IRiderRegistrationConfigurator<TBus>
    where TBus : class, IBus
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="collection">The collection value.</param>
    /// <param name="registrar">The registrar value.</param>
    public ServiceCollectionRiderConfigurator(IServiceCollection collection, IContainerRegistrar registrar)
        : base(collection, registrar)
    {
    }

    /// <summary>
    /// Performs the try add scoped operation.
    /// </summary>
    /// <typeparam name="TRider">The t rider type.</typeparam>
    /// <typeparam name="TService">The t service type.</typeparam>
    /// <param name="factory">The factory value.</param>
    public override void TryAddScoped<TRider, TService>(Func<TRider, IServiceProvider, TService> factory)
    {
        Services.TryAddScoped(provider => factory(provider.GetRequiredService<Bind<TBus, TRider>>().Value, provider));
    }

    /// <summary>
    /// Sets rider factory.
    /// </summary>
    /// <typeparam name="TRider">The t rider type.</typeparam>
    /// <param name="riderFactory">The rider factory value.</param>
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
        Services.AddSingleton(provider =>
            Bind<TBus>.Create(riderFactory.CreateRider(provider.GetRequiredService<Bind<TBus, TRider, IRiderRegistrationContext>>().Value)));
        Services.AddSingleton(provider => Bind<TBus>.Create(provider.GetRequiredService<IBusInstance<TBus>>().GetRider<TRider>()));
    }
}
