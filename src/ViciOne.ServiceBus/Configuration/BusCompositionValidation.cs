using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ViciOne.ServiceBus.Providers.Configuration;
using ViciOne.ServiceBus.Providers.Persistence;


namespace ViciOne.ServiceBus.Configuration;

internal interface IBusCompositionRegistration
{
    Type BusType { get; }

    string BusKey { get; }

    IServiceCollection Services { get; }
}

internal sealed class BusCompositionRegistration<TBus>(IServiceCollection services) : IBusCompositionRegistration
    where TBus : class, IBus
{
    public Type BusType => typeof(TBus);

    public string BusKey { get; } = BusRegistrationIdentity.GetKey(typeof(TBus));

    public IServiceCollection Services { get; } = services ?? throw new ArgumentNullException(nameof(services));
}

internal interface IBusTransportRegistration
{
    Type BusType { get; }

    string BusKey { get; }

    Type FactoryType { get; }
}

internal sealed class BusTransportRegistration<TBus>(Type factoryType) : IBusTransportRegistration
    where TBus : class, IBus
{
    public Type BusType => typeof(TBus);

    public string BusKey { get; } = BusRegistrationIdentity.GetKey(typeof(TBus));

    public Type FactoryType { get; } = factoryType ?? throw new ArgumentNullException(nameof(factoryType));
}

internal interface IBusFeatureRegistration
{
    Type BusType { get; }

    string BusKey { get; }

    string Feature { get; }
}

internal sealed class BusFeatureRegistration<TBus>(string feature) : IBusFeatureRegistration
    where TBus : class, IBus
{
    public Type BusType => typeof(TBus);

    public string BusKey { get; } = BusRegistrationIdentity.GetKey(typeof(TBus));

    public string Feature { get; } = string.IsNullOrWhiteSpace(feature)
        ? throw new ArgumentException("Feature must not be empty.", nameof(feature))
        : feature;
}

internal static class BusCompositionRegistrations
{
    public static void AddBus<TBus>(IServiceCollection services)
        where TBus : class, IBus
    {
        ArgumentNullException.ThrowIfNull(services);
        if (services.Any(static descriptor =>
                descriptor.ServiceType == typeof(IBusCompositionRegistration)
                && descriptor.ImplementationInstance is IBusCompositionRegistration registration
                && registration.BusType == typeof(TBus)))
            return;

        services.AddSingleton<IBusCompositionRegistration>(new BusCompositionRegistration<TBus>(services));
    }

    public static void AddTransport<TBus>(IServiceCollection services, Type factoryType)
        where TBus : class, IBus
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(factoryType);
        services.AddSingleton<IBusTransportRegistration>(new BusTransportRegistration<TBus>(factoryType));
    }

    public static void AddFeature<TBus>(IServiceCollection services, string feature, bool allowMultipleDeclarations = false)
        where TBus : class, IBus
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(feature);

        bool exists = services.Any(descriptor =>
            descriptor.ServiceType == typeof(IBusFeatureRegistration)
            && descriptor.ImplementationInstance is IBusFeatureRegistration registration
            && registration.BusType == typeof(TBus)
            && string.Equals(registration.Feature, feature, StringComparison.Ordinal));
        if (!exists || !allowMultipleDeclarations)
            services.AddSingleton<IBusFeatureRegistration>(new BusFeatureRegistration<TBus>(feature));
    }
}

/// <summary>
/// Aggregates static ownership failures for one bus before the runtime host starts any bus or delivery service.
/// </summary>
internal sealed class BusCompositionStartupValidator<TBus>(IServiceProvider provider) : IHostedService
    where TBus : class, IBus
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string bus = BusRegistrationIdentity.GetKey(typeof(TBus));
        List<string> failures = [];
        IBusCompositionRegistration[] buses = provider.GetServices<IBusCompositionRegistration>().ToArray();
        IBusCompositionRegistration registration = buses.Single(candidate => candidate.BusType == typeof(TBus));
        IBusTransportRegistration[] transports = provider.GetServices<IBusTransportRegistration>()
            .Where(static registration => registration.BusType == typeof(TBus))
            .ToArray();
        IMessageLimitsRegistration[] limits = provider.GetServices<IMessageLimitsRegistration>()
            .Where(registration => string.Equals(registration.BusKey, bus, StringComparison.Ordinal))
            .ToArray();
        EndpointQosConfigurationException? endpointQosFailure = null;

        AddCardinalityFailure(
            failures,
            "Transport",
            bus,
            transports.Length,
            "no transport is selected",
            "multiple transport owners are selected",
            "Select exactly one transport inside the bus block");
        AddCardinalityFailure(
            failures,
            "Message limits",
            bus,
            limits.Length,
            "MaxBodyBytes is not declared",
            "Limits has multiple owners",
            "Call bus.Limits(...) with explicit byte limits");

        Type[] registeredBusTypes = buses.Select(static registration => registration.BusType).Distinct().ToArray();
        IBusFeatureRegistration[] features = provider.GetServices<IBusFeatureRegistration>().ToArray();
        foreach (IBusFeatureRegistration orphan in features
                     .Where(registration => !registeredBusTypes.Contains(registration.BusType))
                     .OrderBy(static registration => registration.BusKey, StringComparer.Ordinal)
                     .ThenBy(static registration => registration.Feature, StringComparer.Ordinal))
        {
            failures.Add(ConfigurationMessages.Create(
                orphan.Feature,
                orphan.BusKey,
                $"the feature targets {orphan.BusType.FullName}, but no matching bus is registered",
                "Move the feature into the matching AddViciOneServiceBus<TBus>(...) block or register that bus"));
        }

        foreach (IGrouping<string, IBusFeatureRegistration> duplicate in features
                     .Where(static registration => registration.BusType == typeof(TBus))
                     .GroupBy(static registration => registration.Feature, StringComparer.Ordinal)
                     .Where(static group => group.Count() > 1)
                     .OrderBy(static group => group.Key, StringComparer.Ordinal))
        {
            failures.Add(ConfigurationMessages.Create(
                duplicate.Key,
                bus,
                "multiple feature owners are registered",
                "Configure the feature exactly once inside its owning bus block"));
        }

        bool durableSenderConfigured = features.Any(registration =>
            registration.BusType == typeof(TBus)
            && string.Equals(registration.Feature, "Reliable messaging", StringComparison.Ordinal));
        if (durableSenderConfigured)
        {
            AddCardinalityFailure(
                failures,
                "Reliable messaging",
                bus,
                CountServices(registration, typeof(IMessageContractCatalog)),
                "no message-contract catalog is registered",
                "multiple message-contract catalog owners are registered",
                "Declare contracts once inside the owning bus.UseReliableMessaging(...) or bus.Contracts(...) block");
            AddCardinalityFailure(
                failures,
                "Reliable messaging",
                bus,
                CountServices(registration, typeof(IOutboxStore<TBus>)),
                "no persistence store is registered",
                "multiple persistence store owners are registered",
                "Select exactly one store inside bus.UseReliableMessaging(...)"
            );
            AddCardinalityFailure(
                failures,
                "Reliable messaging",
                bus,
                CountServices(registration, typeof(IInboxStore<TBus>)),
                "no inbox store is registered",
                "multiple inbox store owners are registered",
                "Select exactly one store inside bus.UseReliableMessaging(...)"
            );
            AddCardinalityFailure(
                failures,
                "Reliable messaging",
                bus,
                CountServices(registration, typeof(IScheduleStore<TBus>)),
                "no schedule store is registered",
                "multiple schedule store owners are registered",
                "Select exactly one store inside bus.UseReliableMessaging(...)"
            );
            AddCardinalityFailure(
                failures,
                "Reliable messaging",
                bus,
                CountServices(registration, typeof(IDurableSendDispatcher<TBus>)),
                "no transport dispatcher is registered",
                "multiple transport dispatcher owners are registered",
                "Select a transport with a durable sender provider or configure exactly one dispatcher"
            );

            if (CountServices(registration, typeof(IMessageContractCatalog)) == 1)
            {
                try
                {
                    _ = provider.GetRequiredService<IMessageContractCatalog>();
                }
                catch (Exception exception)
                {
                    failures.Add(ConfigurationMessages.Create(
                        "Reliable messaging",
                        bus,
                        Unwrap(exception).Message,
                        "Declare a coherent contract catalog inside the owning bus block"));
                }
            }
        }

        if (failures.Count == 0)
        {
            try
            {
                _ = provider.GetRequiredService<TBus>();
            }
            catch (Exception exception)
            {
                Exception cause = Unwrap(exception);
                string message = ConfigurationMessages.Create(
                    cause is EndpointQosConfigurationException ? "Endpoint QoS" : "Bus composition",
                    bus,
                    cause.Message,
                    "Correct the named configuration before starting the host");
                failures.Add(message);
                if (cause is EndpointQosConfigurationException)
                    endpointQosFailure = new EndpointQosConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Bus Composition Startup Validator", "unknown", message, "Correct the named configuration before starting the host"));
            }
        }

        if (failures.Count > 0)
        {
            if (endpointQosFailure is not null && failures.Count == 1)
                throw endpointQosFailure;
            throw new ConfigurationException(ConfigurationMessages.Aggregate(failures));
        }

        return Task.CompletedTask;
    }

    Task IHostedService.StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    static int CountServices(IBusCompositionRegistration registration, Type serviceType)
        => registration.Services.Count(descriptor => descriptor.ServiceType == serviceType);

    static void AddCardinalityFailure(
        ICollection<string> failures,
        string feature,
        string bus,
        int count,
        string missing,
        string ambiguous,
        string fix)
    {
        if (count == 1)
            return;

        failures.Add(ConfigurationMessages.Create(feature, bus, count == 0 ? missing : ambiguous, fix));
    }

    static Exception Unwrap(Exception exception)
    {
        Exception current = exception;
        while (current is AggregateException { InnerExceptions.Count: 1 } aggregate)
            current = aggregate.InnerExceptions[0];
        while (current.InnerException is not null
               && current is not EndpointQosConfigurationException
               && (current is ConfigurationException || current is InvalidOperationException))
            current = current.InnerException;
        return current;
    }
}
