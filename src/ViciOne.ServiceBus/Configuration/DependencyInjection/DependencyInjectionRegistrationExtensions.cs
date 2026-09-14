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
using ViciOne.ServiceBus.Hosting;
using ViciOne.ServiceBus.Monitoring.Health;
using ViciOne.ServiceBus.Providers.Persistence;
using ViciOne.ServiceBus.Transports;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Registers default and typed buses with dependency injection.</summary>
public static class DependencyInjectionRegistrationExtensions
{
    /// <summary>Registers the default bus and applies its endpoint, consumer, saga, activity, and transport configuration.</summary>
    /// <param name="collection">The service collection that will own the bus.</param>
    /// <param name="configure">An optional callback that configures the bus registration.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddViciOneServiceBus(this IServiceCollection collection, Action<IBusRegistrationConfigurator>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(collection);

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

    /// <summary>Registers a typed bus through an explicit bus-instance implementation.</summary>
    /// <typeparam name="TBus">The application-facing bus contract.</typeparam>
    /// <typeparam name="TBusInstance">The implementation that binds the bus contract to its runtime instance.</typeparam>
    /// <param name="collection">The service collection that will own the bus.</param>
    /// <param name="configure">The callback that configures the typed bus registration.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddViciOneServiceBus<TBus, TBusInstance>(this IServiceCollection collection,
        Action<IBusRegistrationConfigurator<TBus>> configure)
        where TBus : class, IBus
        where TBusInstance : BusInstance<TBus>, TBus
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(configure);

        if (collection.Any(d => d.ServiceType == typeof(TBus)))
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Dependency Injection Registration Extensions", "unknown", $"AddViciOneServiceBus<{typeof(TBus).Name},{typeof(TBusInstance).Name}>() was already called and may only be called once per container. To configure additional bus instances, refer to the documentation: https://github.com/EdgarLiebold/vicione-servicebus/usage/containers/multibus.html", "Correct the named configuration before starting the host"));
        }

        AddHostedService<TBus>(collection);
        AddInstrumentation(collection);
        collection.AddSingleton(BusPersistenceIdentity<TBus>.Unspecified);

        var configurator = new ServiceCollectionBusConfigurator<TBus, TBusInstance>(collection);

        configure(configurator);

        configurator.Complete();

        return collection;
    }

    /// <summary>Registers a typed bus whose persistent features share an identity that remains stable across type renames.</summary>
    /// <typeparam name="TBus">The application-facing bus contract.</typeparam>
    /// <typeparam name="TBusInstance">The implementation that binds the bus contract to its runtime instance.</typeparam>
    /// <param name="collection">The service collection that will own the bus.</param>
    /// <param name="persistenceIdentity">The stable identity used to partition all durable state owned by the bus.</param>
    /// <param name="configure">The callback that configures the typed bus registration.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddViciOneServiceBus<TBus, TBusInstance>(
        this IServiceCollection collection,
        string persistenceIdentity,
        Action<IBusRegistrationConfigurator<TBus>> configure)
        where TBus : class, IBus
        where TBusInstance : BusInstance<TBus>, TBus
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(configure);
        BusPersistenceIdentity<TBus> identity = BusPersistenceIdentity<TBus>.Create(persistenceIdentity);
        return collection.AddViciOneServiceBus<TBus, TBusInstance>(configurator =>
        {
            collection.RemoveAll<BusPersistenceIdentity<TBus>>();
            collection.AddSingleton(identity);
            configure(configurator);
        });
    }

    /// <summary>Registers a typed bus and creates the internal bus-instance binding for its contract.</summary>
    /// <typeparam name="TBus">The application-facing bus contract.</typeparam>
    /// <param name="collection">The service collection that will own the bus.</param>
    /// <param name="configure">The callback that configures the typed bus registration.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddViciOneServiceBus<TBus>(this IServiceCollection collection, Action<IBusRegistrationConfigurator<TBus>> configure)
        where TBus : class, IBus
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(configure);

        var doIt = new Callback<TBus>(collection, configure);

        BusInstanceBuilder.Instance.GetBusInstanceType(doIt);

        return collection;
    }

    /// <summary>Registers a typed bus whose persistent features share an identity that remains stable across type renames.</summary>
    /// <typeparam name="TBus">The application-facing bus contract.</typeparam>
    /// <param name="collection">The service collection that will own the bus.</param>
    /// <param name="persistenceIdentity">The stable identity used to partition all durable state owned by the bus.</param>
    /// <param name="configure">The callback that configures the typed bus registration.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddViciOneServiceBus<TBus>(
        this IServiceCollection collection,
        string persistenceIdentity,
        Action<IBusRegistrationConfigurator<TBus>> configure)
        where TBus : class, IBus
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(configure);
        BusPersistenceIdentity<TBus> identity = BusPersistenceIdentity<TBus>.Create(persistenceIdentity);
        return collection.AddViciOneServiceBus<TBus>(configurator =>
        {
            collection.RemoveAll<BusPersistenceIdentity<TBus>>();
            collection.AddSingleton(identity);
            configure(configurator);
        });
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
                && descriptor.ImplementationType == typeof(ServiceBusHostedService));
            var validator = ServiceDescriptor.Singleton(typeof(IHostedService), validatorType);
            if (runtimeIndex < 0)
                collection.Add(validator);
            else
                collection.Insert(runtimeIndex, validator);
        }

        collection.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, ServiceBusHostedService>());
    }

    class Callback<TBus> :
        IBusInstanceBuilderCallback<TBus, IServiceCollection>
        where TBus : class, IBus
    {
        readonly Action<IBusRegistrationConfigurator<TBus>> _configure;
        readonly IServiceCollection _services;

        public Callback(IServiceCollection services, Action<IBusRegistrationConfigurator<TBus>> configure)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _configure = configure ?? throw new ArgumentNullException(nameof(configure));
        }

        public IServiceCollection GetResult<TBusInstance>()
            where TBusInstance : BusInstance<TBus>, TBus
        {
            return _services.AddViciOneServiceBus<TBus, TBusInstance>(_configure);
        }
    }
}
