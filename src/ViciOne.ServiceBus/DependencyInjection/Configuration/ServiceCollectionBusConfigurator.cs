using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.Clients;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Courier;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a service collection bus configurator implementation.
/// </summary>
public class ServiceCollectionBusConfigurator :
    RegistrationConfigurator,
    IBusRegistrationConfigurator,
    IAdvancedBusRegistrationConfigurator
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="collection">The collection value.</param>
    public ServiceCollectionBusConfigurator(IServiceCollection collection)
        : this(collection, new DependencyInjectionContainerRegistrar(collection))
    {
        IBusRegistrationContext CreateRegistrationContext(IServiceProvider provider)
        {
            var setter = provider.GetRequiredService<Bind<IBus, ISetScopedConsumeContext>>();
            return new BusRegistrationContext(provider, Registrar, setter.Value, typeof(IBus));
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

        collection.TryAddSingleton(provider => Bind<IBus>.Create(CreateClientFactory(provider.GetRequiredService<IBus>(), DefaultRequestTimeout)));
        collection.TryAddSingleton(provider => provider.GetRequiredService<Bind<IBus, IClientFactory>>().Value);

        collection.TryAddScoped<IScopedBusContextProvider<IBus>, ScopedBusContextProvider<IBus>>();
        collection.TryAddScoped(provider => Bind<IBus>.Create(provider.GetRequiredService<IScopedBusContextProvider<IBus>>().Context.SendEndpointProvider));
        collection.TryAddScoped(provider => Bind<IBus>.Create(provider.GetRequiredService<IScopedBusContextProvider<IBus>>().Context.PublishEndpoint));

        collection.TryAddScoped<IRoutingSlipExecutor>(provider => new RoutingSlipExecutor(
            provider.GetRequiredService<IScopedBusContextProvider<IBus>>().Context.SendEndpointProvider,
            provider.GetRequiredService<IScopedBusContextProvider<IBus>>().Context.PublishEndpoint,
            provider.GetService<TimeProvider>() ?? TimeProvider.System));
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="collection">The collection value.</param>
    /// <param name="registrar">The registrar value.</param>
    protected ServiceCollectionBusConfigurator(IServiceCollection collection, IContainerRegistrar registrar)
        : base(collection, registrar)
    {
        AddViciOneServiceBusComponents(collection);
    }

    /// <summary>
    /// Gets or sets the create client factory value.
    /// </summary>
    protected Func<IBus, RequestTimeout, IClientFactory> CreateClientFactory { get; private set; } = DefaultClientFactory;

    /// <summary>
    /// Sets bus factory.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="busFactory">The bus factory value.</param>
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

    /// <summary>
    /// Adds rider to the configuration.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    public virtual void AddRider(Action<IRiderRegistrationConfigurator> configure)
    {
        var configurator = new ServiceCollectionRiderConfigurator(Services, new DependencyInjectionRiderContainerRegistrar<IBus>(Services));
        configure?.Invoke(configurator);
    }

    /// <summary>
    /// Adds configure endpoints callback to the configuration.
    /// </summary>
    /// <param name="callback">The callback value.</param>
    public virtual void AddConfigureEndpointsCallback(ConfigureEndpointsCallback callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        Services.AddSingleton(_ => Bind<IBus>.Create<IConfigureReceiveEndpoint>(new ConfigureReceiveEndpointDelegate(callback)));
    }

    /// <summary>
    /// Adds configure endpoints callback to the configuration.
    /// </summary>
    /// <param name="callback">The callback value.</param>
    public virtual void AddConfigureEndpointsCallback(ConfigureEndpointsProviderCallback callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        Services.AddSingleton(provider => Bind<IBus>.Create<IConfigureReceiveEndpoint>(new ConfigureReceiveEndpointDelegateProvider(
            provider.GetRequiredService<Bind<IBus, IBusRegistrationContext>>().Value, callback)));
    }

    /// <summary>
    /// Sets request client factory.
    /// </summary>
    /// <param name="clientFactory">The client factory value.</param>
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

        var busInstance = busFactory.CreateBus(provider.GetRequiredService<Bind<IBus, IBusRegistrationContext>>().Value, specifications, string.Empty);

        return busInstance;
    }

    static void AddViciOneServiceBusComponents(IServiceCollection collection)
    {
        collection.TryAddSingleton<IBusDepot, BusDepot>();

        collection.TryAddScoped<ScopedConsumeContextProvider>();
        collection.TryAddScoped<IScopedConsumeContextProvider>(provider => provider.GetRequiredService<ScopedConsumeContextProvider>());

        collection.TryAddScoped(provider => provider.GetRequiredService<IScopedBusContextProvider<IBus>>().Context.SendEndpointProvider);
        collection.TryAddScoped(provider => provider.GetRequiredService<IScopedBusContextProvider<IBus>>().Context.PublishEndpoint);

        collection.TryAddScoped(provider => provider.GetRequiredService<IScopedConsumeContextProvider>().GetContext() ?? MissingConsumeContext.Instance);

        collection.TryAddScoped(typeof(IRequestClient<>), typeof(GenericRequestClient<>));
    }

    /// <summary>
    /// This is the default client factory, which can be overridden by configuration
    /// </summary>
    /// <param name="bus"></param>
    /// <param name="timeout"></param>
    /// <returns></returns>
    static IClientFactory DefaultClientFactory(IBus bus, RequestTimeout timeout = default)
    {
        return new ClientFactory(new BusClientFactoryContext(bus, timeout));
    }
}


/// <summary>
/// Provides a service collection bus configurator implementation.
/// </summary>
/// <typeparam name="TBus">The t bus type.</typeparam>
/// <typeparam name="TBusInstance">The t bus instance type.</typeparam>
public class ServiceCollectionBusConfigurator<TBus, TBusInstance> :
    ServiceCollectionBusConfigurator,
    IBusRegistrationConfigurator<TBus>,
    IAdvancedBusRegistrationConfigurator<TBus>
    where TBus : class, IBus
    where TBusInstance : BusInstance<TBus>, TBus
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="collection">The collection value.</param>
    public ServiceCollectionBusConfigurator(IServiceCollection collection)
        : base(collection, new DependencyInjectionContainerRegistrar<TBus>(collection))
    {
        IBusRegistrationContext CreateRegistrationContext(IServiceProvider provider)
        {
            var setter = provider.GetRequiredService<Bind<TBus, ISetScopedConsumeContext>>();
            return new BusRegistrationContext(provider, Registrar, setter.Value, typeof(TBus));
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
        collection.TryAddSingleton(provider => Bind<TBus>.Create(CreateClientFactory(provider.GetRequiredService<TBus>(), DefaultRequestTimeout)));

        collection.TryAddScoped<IScopedBusContextProvider<TBus>, ScopedBusContextProvider<TBus>>();
        collection.TryAddScoped(provider => Bind<TBus>.Create(provider.GetRequiredService<IScopedBusContextProvider<TBus>>().Context.SendEndpointProvider));
        collection.TryAddScoped(provider => Bind<TBus>.Create(provider.GetRequiredService<IScopedBusContextProvider<TBus>>().Context.PublishEndpoint));

        collection.TryAddScoped(provider => Bind<TBus>.Create<IRoutingSlipExecutor>(new RoutingSlipExecutor(
            provider.GetRequiredService<IScopedBusContextProvider<TBus>>().Context.SendEndpointProvider,
            provider.GetRequiredService<IScopedBusContextProvider<TBus>>().Context.PublishEndpoint,
            provider.GetService<TimeProvider>() ?? TimeProvider.System)));

        collection.AddSingleton(provider => Bind<TBus>.Create(CreateRegistrationContext(provider)));
    }

    /// <summary>
    /// Sets bus factory.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="busFactory">The bus factory value.</param>
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

    /// <summary>
    /// Adds rider to the configuration.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    public override void AddRider(Action<IRiderRegistrationConfigurator> configure)
    {
        AddRider(configurator => configure.Invoke(configurator));
    }

    /// <summary>
    /// Adds rider to the configuration.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    public void AddRider(Action<IRiderRegistrationConfigurator<TBus>> configure)
    {
        var configurator = new ServiceCollectionRiderConfigurator<TBus>(Services, new DependencyInjectionRiderContainerRegistrar<TBus>(Services));
        configure?.Invoke(configurator);
    }

    /// <summary>
    /// Adds configure endpoints callback to the configuration.
    /// </summary>
    /// <param name="callback">The callback value.</param>
    public override void AddConfigureEndpointsCallback(ConfigureEndpointsCallback callback)
    {
        Services.AddSingleton(_ => Bind<TBus>.Create<IConfigureReceiveEndpoint>(new ConfigureReceiveEndpointDelegate(callback)));
    }

    /// <summary>
    /// Adds configure endpoints callback to the configuration.
    /// </summary>
    /// <param name="callback">The callback value.</param>
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

        var instance = busFactory.CreateBus(provider.GetRequiredService<Bind<TBus, IBusRegistrationContext>>().Value, specifications, typeof(TBus).Name);

        var busInstance = provider.GetService<TBusInstance>() ?? ActivatorUtilities.CreateInstance<TBusInstance>(provider, instance.BusControl);

        return new MultiBusInstance<TBus>(busInstance, instance);
    }
}
