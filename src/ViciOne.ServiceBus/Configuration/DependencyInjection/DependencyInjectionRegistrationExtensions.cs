using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Monitoring;
using ViciOne.ServiceBus.Providers.Persistence;
using ViciOne.ServiceBus.Transports;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Standard registration extensions, which are used to configure consumers, sagas, and activities on receive endpoints from a
/// dependency injection container.
/// </summary>
public static class DependencyInjectionRegistrationExtensions
{
    /// <summary>
    /// Adds ViciOne.ServiceBus and its dependencies to the <paramref name="collection" />, and allows consumers, sagas, and activities to be configured
    /// </summary>
    /// <param name="collection"></param>
    /// <param name="configure"></param>
    public static IServiceCollection AddViciOneServiceBus(this IServiceCollection collection, Action<IBusRegistrationConfigurator>? configure = null)
    {
        if (collection.Any(d => d.ServiceType == typeof(IBus)))
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Dependency Injection Registration Extensions", "unknown", "AddViciOneServiceBus() was already called and may only be called once per container. To configure additional bus instances, refer to the documentation: https://github.com/EdgarLiebold/vicione-servicebus/usage/containers/multibus.html", "Correct the named configuration before starting the host"));
        }

        AddHostedService<IBus>(collection);
        AddInstrumentation(collection);
        collection.AddSingleton(BusPersistenceIdentity<IBus>.Default);

        var configurator = new ServiceCollectionBusConfigurator(collection);

        configure?.Invoke(configurator);

        configurator.Complete();

        return collection;
    }

    /// <summary>
    /// Configure a ViciOne.ServiceBus bus instance, using the specified <typeparamref name="TBus" /> bus type, which must inherit directly from <see cref="IBus" />.
    /// A type that implements <typeparamref name="TBus" /> is required, specified by the <typeparamref name="TBusInstance" /> parameter.
    /// </summary>
    /// <param name="collection">The service collection</param>
    /// <param name="configure">Bus instance configuration method</param>
    public static IServiceCollection AddViciOneServiceBus<TBus, TBusInstance>(this IServiceCollection collection,
        Action<IBusRegistrationConfigurator<TBus>> configure)
        where TBus : class, IBus
        where TBusInstance : BusInstance<TBus>, TBus
    {
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        if (collection.Any(d => d.ServiceType == typeof(TBus)))
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Dependency Injection Registration Extensions", "unknown", $"AddViciOneServiceBus<{typeof(TBus).Name},{typeof(TBusInstance).Name}>() was already called and may only be called once per container. To configure additional bus instances, refer to the documentation: https://github.com/EdgarLiebold/vicione-servicebus/usage/containers/multibus.html", "Correct the named configuration before starting the host"));
        }

        AddHostedService<TBus>(collection);
        AddInstrumentation(collection);
        collection.AddSingleton(BusPersistenceIdentity<TBus>.Unspecified);

        var configurator = new ServiceCollectionBusConfigurator<TBus, TBusInstance>(collection);

        configure?.Invoke(configurator);

        configurator.Complete();

        return collection;
    }

    /// <summary>
    /// Configures a typed bus with the one rename-stable identity shared by all of its persistent features.
    /// </summary>
    public static IServiceCollection AddViciOneServiceBus<TBus, TBusInstance>(
        this IServiceCollection collection,
        string persistenceIdentity,
        Action<IBusRegistrationConfigurator<TBus>> configure)
        where TBus : class, IBus
        where TBusInstance : BusInstance<TBus>, TBus
    {
        ArgumentNullException.ThrowIfNull(configure);
        BusPersistenceIdentity<TBus> identity = BusPersistenceIdentity<TBus>.Create(persistenceIdentity);
        return collection.AddViciOneServiceBus<TBus, TBusInstance>(configurator =>
        {
            collection.RemoveAll<BusPersistenceIdentity<TBus>>();
            collection.AddSingleton(identity);
            configure(configurator);
        });
    }

    /// <summary>
    /// Configure a ViciOne.ServiceBus MultiBus instance, using the specified <typeparamref name="TBus" /> bus type, which must inherit directly from <see cref="IBus" />.
    /// A dynamic type will be created to support the bus instance, which will be initialized when the <typeparamref name="TBus" /> type is retrieved
    /// from the container.
    /// </summary>
    /// <param name="collection">The service collection</param>
    /// <param name="configure">Bus instance configuration method</param>
    public static IServiceCollection AddViciOneServiceBus<TBus>(this IServiceCollection collection, Action<IBusRegistrationConfigurator<TBus>> configure)
        where TBus : class, IBus
    {
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        var doIt = new Callback<TBus>(collection, configure);

        BusInstanceBuilder.Instance.GetBusInstanceType(doIt);

        return collection;
    }

    /// <summary>
    /// Configures a typed bus with the one rename-stable identity shared by all of its persistent features.
    /// </summary>
    public static IServiceCollection AddViciOneServiceBus<TBus>(
        this IServiceCollection collection,
        string persistenceIdentity,
        Action<IBusRegistrationConfigurator<TBus>> configure)
        where TBus : class, IBus
    {
        ArgumentNullException.ThrowIfNull(configure);
        BusPersistenceIdentity<TBus> identity = BusPersistenceIdentity<TBus>.Create(persistenceIdentity);
        return collection.AddViciOneServiceBus<TBus>(configurator =>
        {
            collection.RemoveAll<BusPersistenceIdentity<TBus>>();
            collection.AddSingleton(identity);
            configure(configurator);
        });
    }

    /// <summary>
    /// In some situations, it may be necessary to Remove the ViciOneServiceBusHostedService from the container, such as
    /// when using older versions of the Azure Functions runtime.
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection RemoveViciOneServiceBusHostedService(this IServiceCollection services)
    {
        return RemoveHostedService<ViciOneServiceBusHostedService>(services);
    }

    /// <summary>
    /// Remove the specified hosted service from the service collection
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection RemoveHostedService<T>(this IServiceCollection services)
        where T : IHostedService
    {
        var descriptor = services.FirstOrDefault(x => x.ServiceType == typeof(IHostedService) && x.ImplementationType == typeof(T));
        if (descriptor != null)
            services.Remove(descriptor);

        return services;
    }

    /// <summary>
    /// Replace a scoped service registration with a new one
    /// </summary>
    /// <typeparam name="TService"></typeparam>
    /// <typeparam name="TImplementation"></typeparam>
    public static void ReplaceScoped<TService, TImplementation>(this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        services.Replace(new ServiceDescriptor(typeof(TService), typeof(TImplementation), ServiceLifetime.Scoped));
    }

    static void AddInstrumentation(IServiceCollection collection)
    {
        // The provider isolated metric path resolves its meter factory from the container. Registering the
        // standard metric core here is idempotent and does not replace a factory an application registered
        // itself, so the core does not silently expect the application bootstrap to do it.
        collection.AddMetrics();
    }

    static void AddHostedService<TBus>(IServiceCollection collection)
        where TBus : class, IBus
    {
        BusCompositionRegistrations.AddBus<TBus>(collection);
        collection.AddOptions();
        collection.AddHealthChecks();
        collection.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<HealthCheckServiceOptions>, ConfigureBusHealthCheckServiceOptions>());

        collection.AddOptions<ViciOneServiceBusHostOptions>()
            .ValidateOnStart();
        collection.TryAddSingleton<IValidateOptions<ViciOneServiceBusHostOptions>, ValidateViciOneServiceBusHostOptions>();

        Type validatorType = typeof(BusCompositionStartupValidator<>).MakeGenericType(typeof(TBus));
        if (!collection.Any(descriptor => descriptor.ServiceType == typeof(IHostedService)
                && descriptor.ImplementationType == validatorType))
        {
            int runtimeIndex = collection.ToList().FindIndex(descriptor => descriptor.ServiceType == typeof(IHostedService)
                && descriptor.ImplementationType == typeof(ViciOneServiceBusHostedService));
            var validator = ServiceDescriptor.Singleton(typeof(IHostedService), validatorType);
            if (runtimeIndex < 0)
                collection.Add(validator);
            else
                collection.Insert(runtimeIndex, validator);
        }

        collection.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, ViciOneServiceBusHostedService>());
    }

    internal static void RemoveViciOneServiceBus(this IServiceCollection collection)
    {
        collection.RemoveAll<IClientFactory>();
        collection.RemoveAll<Bind<IBus, IBusRegistrationContext>>();
        collection.RemoveAll<IBusRegistrationContext>();
        collection.RemoveAll(typeof(IReceiveEndpointDispatcher<>));
        collection.RemoveAll<IReceiveEndpointDispatcherFactory>();


        collection.RemoveAll<IBusDepot>();
        collection.RemoveAll<IScopedConsumeContextProvider>();
        collection.RemoveAll<Bind<IBus, ISetScopedConsumeContext>>();
        collection.RemoveAll<Bind<IBus, IScopedConsumeContextProvider>>();
        collection.RemoveAll<IScopedBusContextProvider<IBus>>();
        collection.RemoveAll<ConsumeContext>();
        collection.RemoveAll<ISendEndpointProvider>();
        collection.RemoveAll<IPublishEndpoint>();
        collection.RemoveAll(typeof(IRequestClient<>));
        collection.RemoveAll<IMessageScheduler>();

        collection.RemoveAll<Bind<IBus, IBusInstance>>();
        collection.RemoveAll<IBusInstance>();
        collection.RemoveAll<IReceiveEndpointConnector>();
        collection.RemoveAll<IBusControl>();
        collection.RemoveAll<IBus>();

        collection.RemoveAll<IScopedClientFactory>();
    }


    class Callback<TBus> :
        IBusInstanceBuilderCallback<TBus, IServiceCollection>
        where TBus : class, IBus
    {
        readonly Action<IBusRegistrationConfigurator<TBus>> _configure;
        readonly IServiceCollection _services;

        public Callback(IServiceCollection services, Action<IBusRegistrationConfigurator<TBus>> configure)
        {
            _services = services;
            _configure = configure;
        }

        public IServiceCollection GetResult<TBusInstance>()
            where TBusInstance : BusInstance<TBus>, TBus
        {
            return _services.AddViciOneServiceBus<TBus, TBusInstance>(_configure);
        }
    }
}
