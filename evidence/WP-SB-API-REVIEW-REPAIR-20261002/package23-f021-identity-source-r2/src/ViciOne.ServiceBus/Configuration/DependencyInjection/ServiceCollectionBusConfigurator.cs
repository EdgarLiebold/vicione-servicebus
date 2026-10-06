using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Clients;
using ViciOne.ServiceBus.Clients.Contexts;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.Providers.Persistence;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Builds the default bus registration graph in a service collection.</summary>
public class ServiceCollectionBusConfigurator :
    RegistrationConfigurator,
    IBusRegistrationConfigurator,
    IAdvancedBusRegistrationConfigurator
{
    /// <summary>Creates a default-bus configurator and registers its core scoped services.</summary>
    /// <param name="collection">The service collection that owns the bus.</param>
    public ServiceCollectionBusConfigurator(IServiceCollection collection)
        : this(collection, new DependencyInjectionContainerRegistrar(collection))
    {
        IBusRegistrationContext CreateRegistrationContext(IServiceProvider provider)
        {
            var setter = provider.GetRequiredService<Bind<IBus, ISetScopedConsumeContext>>();
            return new BusRegistrationContext(provider, Registrar, setter.Value, typeof(IBus),
                (provider.GetService<BusPersistenceIdentity<IBus>>() ?? BusPersistenceIdentity<IBus>.Default).Require("Classic EF inbox"));
        }

        static Bind<IBus, IScopedConsumeContextProvider> CreateScopeProvider(IServiceProvider provider)
        {
            var global = provider.GetRequiredService<IScopedConsumeContextProvider>();
            return Bind<IBus>.Create((IScopedConsumeContextProvider)new TypedScopedConsumeContextProvider(global));
        }

        collection.AddScoped(CreateScopeProvider);
        collection.AddSingleton(_ =>
            Bind<IBus>.Create((ISetScopedConsumeContext)new SetScopedConsumeContext<IBus>(provider =>
                provider.GetRequiredService<Bind<IBus, IScopedConsumeContextProvider>>().Value)));

        collection.AddSingleton(provider => Bind<IBus>.Create(CreateRegistrationContext(provider)));
        collection.AddSingleton(provider => provider.GetRequiredService<Bind<IBus, IBusRegistrationContext>>().Value);

        collection.TryAdd(ServiceDescriptor.Singleton(typeof(IReceiveEndpointDispatcher<>), typeof(ReceiveEndpointDispatcher<>)));
        collection.TryAddSingleton<IReceiveEndpointDispatcherFactory>(provider =>
        {
            var context = provider.GetRequiredService<Bind<IBus, IBusRegistrationContext>>().Value;
            var busInstance = provider.GetRequiredService<Bind<IBus, IBusInstance>>().Value;

            return new ReceiveEndpointDispatcherFactory(context, busInstance);
        });

        collection.TryAddSingleton(provider => Bind<IBus>.Create(
            CreateClientFactory(provider.GetRequiredService<IBus>(), DefaultRequestTimeout)
            ?? throw new InvalidOperationException("The request client factory returned null.")));
        collection.TryAddSingleton(provider => provider.GetRequiredService<Bind<IBus, IClientFactory>>().Value);

        collection.TryAddScoped<IScopedBusContextProvider<IBus>, ScopedBusContextProvider<IBus>>();
        collection.TryAddScoped(provider => Bind<IBus>.Create(provider.GetRequiredService<IScopedBusContextProvider<IBus>>().Context.SendEndpointProvider));
        collection.TryAddScoped(provider => Bind<IBus>.Create(provider.GetRequiredService<IScopedBusContextProvider<IBus>>().Context.PublishEndpoint));

    }

    /// <summary>Creates a bus configurator with an explicit container registration strategy.</summary>
    /// <param name="collection">The service collection that owns the bus.</param>
    /// <param name="registrar">The container-specific registration strategy.</param>
    protected ServiceCollectionBusConfigurator(IServiceCollection collection, IContainerRegistrar registrar)
        : base(collection, registrar)
    {
        AddViciOneServiceBusComponents(collection);
    }

    /// <summary>Gets the delegate used to create the bus-owned request client factory.</summary>
    protected Func<IBus, RequestTimeout, IClientFactory> CreateClientFactory { get; private set; } = DefaultClientFactory;

    /// <summary>Sets the transport factory and registers the default bus runtime services.</summary>
    /// <typeparam name="T">The transport-specific registration bus factory.</typeparam>
    /// <param name="busFactory">The factory that creates the bus instance.</param>
    public virtual void SetBusFactory<T>(T busFactory)
        where T : class, IRegistrationBusFactory
    {
        if (busFactory == null)
            throw new ArgumentNullException(nameof(busFactory));

        ThrowIfAlreadyConfigured(nameof(SetBusFactory));
        BusCompositionRegistrations.AddTransport<IBus>(Services, busFactory.GetType());

        Services.AddSingleton(provider => Bind<IBus>.Create(CreateBus(busFactory, provider)));

        Services.AddSingleton(provider => provider.GetRequiredService<Bind<IBus, IBusInstance>>().Value);
        Services.AddSingleton<IReceiveEndpointConnector>(provider => provider.GetRequiredService<Bind<IBus, IBusInstance>>().Value);
        Services.AddSingleton(provider => provider.GetRequiredService<Bind<IBus, IBusInstance>>().Value.BusControl);
        Services.AddSingleton(provider => provider.GetRequiredService<Bind<IBus, IBusInstance>>().Value.Bus);

        Registrar.RegisterScopedClientFactory();
    }

    /// <summary>Registers and completes a rider owned by the default bus.</summary>
    /// <param name="configure">The callback that configures the rider and its services.</param>
    public virtual void AddRider(Action<IRiderRegistrationConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var configurator = new ServiceCollectionRiderConfigurator(Services, new DependencyInjectionRiderContainerRegistrar<IBus>(Services));
        configure(configurator);
        configurator.Complete();
    }

    /// <summary>Adds a callback invoked for every receive endpoint owned by the default bus.</summary>
    /// <param name="callback">The endpoint callback.</param>
    public virtual void AddConfigureEndpointsCallback(ConfigureEndpointsCallback callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        Services.AddSingleton(_ => Bind<IBus>.Create<IConfigureReceiveEndpoint>(new ConfigureReceiveEndpointDelegate(callback)));
    }

    /// <summary>Adds a context-aware callback invoked for every receive endpoint owned by the default bus.</summary>
    /// <param name="callback">The endpoint callback.</param>
    public virtual void AddConfigureEndpointsCallback(ConfigureEndpointsProviderCallback callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        Services.AddSingleton(provider => Bind<IBus>.Create<IConfigureReceiveEndpoint>(new ConfigureReceiveEndpointDelegateProvider(
            provider.GetRequiredService<Bind<IBus, IBusRegistrationContext>>().Value, callback)));
    }

    /// <summary>Replaces the delegate used to construct the default bus request client factory.</summary>
    /// <param name="clientFactory">The delegate that creates a client factory for the resolved bus and timeout.</param>
    public virtual void SetRequestClientFactory(Func<IBus, RequestTimeout, IClientFactory> clientFactory)
    {
        if (clientFactory == null)
            throw new ArgumentNullException(nameof(clientFactory));

        CreateClientFactory = clientFactory;
    }

    static IBusInstance CreateBus<T>(T busFactory, IServiceProvider provider)
        where T : IRegistrationBusFactory
    {
        IEnumerable<IBusInstanceSpecification> specifications = provider.GetServices<Bind<IBus, IBusInstanceSpecification>>().Select(x => x.Value);

        var busInstance = busFactory.CreateBus(provider.GetRequiredService<Bind<IBus, IBusRegistrationContext>>().Value, specifications, string.Empty)
            ?? throw new InvalidOperationException("The bus factory returned null.");

        return busInstance;
    }

    static void AddViciOneServiceBusComponents(IServiceCollection collection)
    {
        collection.TryAddEnumerable(ServiceDescriptor.Singleton<IConsumerKind, ConsumerKind>());

        collection.TryAddSingleton<IBusDepot, BusDepot>();

        collection.TryAddScoped<ScopedConsumeContextProvider>();
        collection.TryAddScoped<IScopedConsumeContextProvider>(provider => provider.GetRequiredService<ScopedConsumeContextProvider>());

        collection.TryAddScoped(provider => provider.GetRequiredService<IScopedBusContextProvider<IBus>>().Context.SendEndpointProvider);
        collection.TryAddScoped(provider => provider.GetRequiredService<IScopedBusContextProvider<IBus>>().Context.PublishEndpoint);

        collection.TryAddScoped(provider => provider.GetRequiredService<IScopedConsumeContextProvider>().GetContext() ?? UnavailableConsumeContext.Instance);

        collection.TryAddScoped(typeof(IRequestClient<>), typeof(GenericRequestClient<>));
    }

    /// <summary>Creates a request client factory directly over the bus endpoint.</summary>
    /// <param name="bus">The bus used to send requests and receive responses.</param>
    /// <param name="timeout">The default response timeout.</param>
    /// <returns>A client factory owned by the bus.</returns>
    static IClientFactory DefaultClientFactory(IBus bus, RequestTimeout timeout = default)
    {
        return new ClientFactory(new BusClientFactoryContext(bus, timeout));
    }
}


/// <summary>Builds an owner-qualified bus registration graph in a service collection.</summary>
/// <typeparam name="TBus">The application-facing bus contract.</typeparam>
/// <typeparam name="TBusInstance">The application-facing bus implementation.</typeparam>
public class ServiceCollectionBusConfigurator<TBus, TBusInstance> :
    ServiceCollectionBusConfigurator,
    IBusRegistrationConfigurator<TBus>,
    IAdvancedBusRegistrationConfigurator<TBus>
    where TBus : class, IBus
    where TBusInstance : BusInstance<TBus>, TBus
{
    /// <inheritdoc />
    public override Type BusType => typeof(TBus);

    /// <summary>Creates a typed-bus configurator and registers its owner-qualified scoped services.</summary>
    /// <param name="collection">The service collection that owns the typed bus.</param>
    public ServiceCollectionBusConfigurator(IServiceCollection collection)
        : base(collection, new DependencyInjectionContainerRegistrar<TBus>(collection))
    {
        IBusRegistrationContext CreateRegistrationContext(IServiceProvider provider)
        {
            var setter = provider.GetRequiredService<Bind<TBus, ISetScopedConsumeContext>>();
            var identity = provider.GetService<BusPersistenceIdentity<TBus>>();
            return new BusRegistrationContext(provider, Registrar, setter.Value, typeof(TBus),
                identity is { IsSpecified: true } ? identity.Require("Classic EF inbox") : null);
        }

        static Bind<TBus, IScopedConsumeContextProvider> CreateScopeProvider(IServiceProvider provider)
        {
            var global = provider.GetRequiredService<IScopedConsumeContextProvider>();
            return Bind<TBus>.Create((IScopedConsumeContextProvider)new TypedScopedConsumeContextProvider(global));
        }

        collection.TryAddScoped(CreateScopeProvider);

        collection.AddSingleton(_ =>
            Bind<TBus>.Create((ISetScopedConsumeContext)new SetScopedConsumeContext<TBus>(provider =>
                provider.GetRequiredService<Bind<TBus, IScopedConsumeContextProvider>>().Value)));
        collection.TryAddSingleton(provider => Bind<TBus>.Create(
            CreateClientFactory(provider.GetRequiredService<TBus>(), DefaultRequestTimeout)
            ?? throw new InvalidOperationException("The request client factory returned null.")));

        collection.TryAddScoped<IScopedBusContextProvider<TBus>, ScopedBusContextProvider<TBus>>();
        collection.TryAddScoped(provider => Bind<TBus>.Create(provider.GetRequiredService<IScopedBusContextProvider<TBus>>().Context.SendEndpointProvider));
        collection.TryAddScoped(provider => Bind<TBus>.Create(provider.GetRequiredService<IScopedBusContextProvider<TBus>>().Context.PublishEndpoint));

        collection.AddSingleton(provider => Bind<TBus>.Create(CreateRegistrationContext(provider)));
    }

    /// <summary>Sets the transport factory and registers the typed bus runtime services.</summary>
    /// <typeparam name="T">The transport-specific registration bus factory.</typeparam>
    /// <param name="busFactory">The factory that creates the underlying transport bus instance.</param>
    public override void SetBusFactory<T>(T busFactory)
    {
        if (busFactory == null)
            throw new ArgumentNullException(nameof(busFactory));

        ThrowIfAlreadyConfigured(nameof(SetBusFactory));
        BusCompositionRegistrations.AddTransport<TBus>(Services, busFactory.GetType());

        Services.AddSingleton(provider => CreateBus(busFactory, provider));

        Services.AddSingleton<IBusInstance>(provider => provider.GetRequiredService<IBusInstance<TBus>>());
        Services.AddSingleton(provider => Bind<TBus>.Create<IReceiveEndpointConnector>(provider.GetRequiredService<IBusInstance<TBus>>()));
        Services.AddSingleton(provider => provider.GetRequiredService<IBusInstance<TBus>>().Bus);

        Registrar.RegisterScopedClientFactory();
    }

    /// <summary>Registers and completes a rider through the untyped rider contract.</summary>
    /// <param name="configure">The callback that configures the rider and its services.</param>
    public override void AddRider(Action<IRiderRegistrationConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        AddRider((IRiderRegistrationConfigurator<TBus> configurator) => configure(configurator));
    }

    /// <summary>Registers and completes a rider owned by the typed bus.</summary>
    /// <param name="configure">The callback that configures the owner-qualified rider and its services.</param>
    public void AddRider(Action<IRiderRegistrationConfigurator<TBus>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var configurator = new ServiceCollectionRiderConfigurator<TBus>(Services, new DependencyInjectionRiderContainerRegistrar<TBus>(Services));
        configure(configurator);
        configurator.Complete();
    }

    /// <summary>Adds a callback invoked for every receive endpoint owned by the typed bus.</summary>
    /// <param name="callback">The endpoint callback.</param>
    public override void AddConfigureEndpointsCallback(ConfigureEndpointsCallback callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        Services.AddSingleton(_ => Bind<TBus>.Create<IConfigureReceiveEndpoint>(new ConfigureReceiveEndpointDelegate(callback)));
    }

    /// <summary>Adds a context-aware callback invoked for every receive endpoint owned by the typed bus.</summary>
    /// <param name="callback">The endpoint callback.</param>
    public override void AddConfigureEndpointsCallback(ConfigureEndpointsProviderCallback callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        Services.AddSingleton(provider => Bind<TBus>.Create<IConfigureReceiveEndpoint>(new ConfigureReceiveEndpointDelegateProvider(
            provider.GetRequiredService<Bind<TBus, IBusRegistrationContext>>().Value,
            callback)));
    }

    static IBusInstance<TBus> CreateBus<T>(T busFactory, IServiceProvider provider)
        where T : IRegistrationBusFactory
    {
        IEnumerable<IBusInstanceSpecification> specifications = provider.GetServices<Bind<TBus, IBusInstanceSpecification>>().Select(x => x.Value);

        var instance = busFactory.CreateBus(provider.GetRequiredService<Bind<TBus, IBusRegistrationContext>>().Value, specifications, typeof(TBus).Name)
            ?? throw new InvalidOperationException("The bus factory returned null.");

        var busInstance = provider.GetService<TBusInstance>() ?? ActivatorUtilities.CreateInstance<TBusInstance>(provider, instance.BusControl);

        return new MultiBusInstance<TBus>(busInstance, instance);
    }
}
