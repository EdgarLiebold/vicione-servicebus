using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Providers.Configuration;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Creates transport registration bus instances.</summary>
/// <typeparam name="TEndpointConfigurator">The endpoint configurator type.</typeparam>
public abstract class TransportRegistrationBusFactory<TEndpointConfigurator> :
    IRegistrationBusFactory
    where TEndpointConfigurator : class, IReceiveEndpointConfigurator
{
    readonly IHostConfiguration _hostConfiguration;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="hostConfiguration">The host configuration.</param>
    protected TransportRegistrationBusFactory(IHostConfiguration hostConfiguration)
    {
        _hostConfiguration = hostConfiguration;
    }

    /// <summary>Creates bus.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="specifications">The specifications.</param>
    /// <param name="busName">The bus name.</param>
    /// <returns>The created bus.</returns>
    public abstract IBusInstance CreateBus(IBusRegistrationContext context, IEnumerable<IBusInstanceSpecification> specifications, string busName);

    /// <summary>Creates bus.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TConfigurator">The configurator type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <param name="specifications">The specifications.</param>
    /// <returns>The created bus.</returns>
    protected IBusInstance CreateBus<T, TConfigurator>(T configurator, IBusRegistrationContext context,
        Action<IBusRegistrationContext, TConfigurator>? configure, IEnumerable<IBusInstanceSpecification> specifications)
        where T : TConfigurator, IBusFactory
        where TConfigurator : IBusFactoryConfigurator
    {
        LogContext.ConfigureCurrentLogContextIfNull(context);

        _hostConfiguration.LogContext = LogContext.Current;

        var hostOptions = context.GetService<IOptions<ViciOneServiceBusHostOptions>>()?.Value;
        _hostConfiguration.ConsumerStopTimeout = hostOptions?.ConsumerStopTimeout;
        _hostConfiguration.StopTimeout = hostOptions?.StopTimeout;

        MessageLimits limits = ConfigureMessageLimits(context, _hostConfiguration);
        ConfigurePayloadAdmission(context, _hostConfiguration);
        ConnectBusObservers(context, configurator);
        ConnectMessageJournal(context, configurator);

        configure?.Invoke(context, configurator);

        _hostConfiguration.BusConfiguration.Serialization.ConfigureSystemTextJsonSerializerOptions(options =>
        {
            options.MaxDepth = limits.MaxJsonDepth;
            return options;
        });

        IBusInstanceSpecification[] busInstanceSpecifications = specifications?.ToArray() ?? [];

        IEnumerable<ValidationResult> validationResult = configurator.Validate()
            .Concat(busInstanceSpecifications.SelectMany(x => x.Validate()));

        if (_hostConfiguration.BusConfiguration.MessageRoutes is not MessageRouteTable messageRoutes)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Transport Registration Bus", "unknown", "The bus must own a MessageRouteTable instance.", "Correct the named configuration before starting the host"));

        messageRoutes.Freeze();

        IReadOnlyList<ValidationResult> result = validationResult.ThrowIfContainsFailure("The bus configuration is invalid:");

        try
        {
            var busReceiveEndpointConfiguration = configurator.CreateBusEndpointConfiguration(x =>
            {
                x.ConfigureConsumeTopology = false;

                x.DiscardFaultedMessages();
                x.DiscardSkippedMessages();
            });

            var host = _hostConfiguration.Build() as IHost<TEndpointConfigurator>
                ?? throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Transport Registration Bus", "unknown", $"The configured host does not implement {typeof(IHost<TEndpointConfigurator>)}.", "Correct the named configuration before starting the host"));

            var bus = new ViciOneServiceBusBus(host, _hostConfiguration.BusConfiguration.BusObservers, busReceiveEndpointConfiguration,
                context.GetService<TimeProvider>() ?? TimeProvider.System);

            ConnectReceiveEndpointObservers(context, bus);
            ConnectReceiveObservers(context, bus);
            ConnectConsumeObservers(context, bus);
            ConnectSendObservers(context, bus);
            ConnectPublishObservers(context, bus);

            _hostConfiguration.BusConfiguration.BusObservers.PostCreate(bus);

            var instance = CreateBusInstance(bus, host, _hostConfiguration, context);

            foreach (var specification in busInstanceSpecifications)
                specification.Configure(instance);

            return instance;
        }
        catch (Exception ex)
        {
            _hostConfiguration.BusConfiguration.BusObservers.CreateFaulted(ex);

            throw new ConfigurationException(result, global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Transport Registration Bus", "unknown", "An exception occurred during bus creation", "Correct the named configuration before starting the host"), ex);
        }
    }

    static void ConnectBusObservers(IServiceProvider context, IBusObserverConnector connector)
    {
        foreach (var observer in context.GetServices<IBusObserver>())
            connector.ConnectBusObserver(observer);
    }

    static void ConnectMessageJournal(IBusRegistrationContext context, IBusFactoryConfigurator configurator)
    {
        if (context is not IBusRegistrationIdentity identity)
            return;

        IMessageJournalRegistration[] registrations = context.GetServices<IMessageJournalRegistration>()
            .Where(registration => string.Equals(registration.BusKey, identity.BusKey, StringComparison.Ordinal))
            .ToArray();
        if (registrations.Length > 1)
        {
            throw new ConfigurationException(ConfigurationMessages.Create(
                "Message journal",
                identity.BusKey,
                "multiple journal owners are registered",
                "Configure exactly one bus.UseMessageJournal(...) block"));
        }

        if (registrations.Length == 1)
            registrations[0].Connect(configurator);
    }

    static MessageLimits ConfigureMessageLimits(IBusRegistrationContext context, IHostConfiguration hostConfiguration)
    {
        if (context is not IBusRegistrationIdentity identity)
        {
            throw new ConfigurationException(
                "Message limits for bus 'unknown': Bus identity is unavailable. Register the bus through AddViciOneServiceBus and call bus.Limits(...).");
        }

        IMessageLimitsRegistration[] registrations = context
            .GetServices<IMessageLimitsRegistration>()
            .Where(x => string.Equals(x.BusKey, identity.BusKey, StringComparison.Ordinal))
            .ToArray();

        if (registrations.Length == 0)
        {
            throw new ConfigurationException(
                $"Message limits for bus '{identity.BusKey}': MaxBodyBytes is not declared. Call bus.Limits(...) with explicit byte limits.");
        }

        if (registrations.Length > 1)
        {
            throw new ConfigurationException(
                $"Message limits for bus '{identity.BusKey}': Limits has multiple owners. Configure exactly one Limits policy inside the bus block.");
        }

        if (hostConfiguration is not IMessageLimitsHostConfiguration target)
        {
            throw new ConfigurationException(
                $"Message limits for bus '{identity.BusKey}': The selected transport cannot enforce receive limits. Choose a transport with message-limit support.");
        }

        MessageLimits limits = registrations[0].Limits.Validate(identity.BusKey);
        target.SetMessageLimits(limits);
        return limits;
    }

    static void ConfigurePayloadAdmission(IBusRegistrationContext context, IHostConfiguration hostConfiguration)
    {
        if (context is not IBusRegistrationIdentity identity)
            return;

        IPayloadAdmissionRuntimeRegistration[] registrations = context
            .GetServices<IPayloadAdmissionRuntimeRegistration>()
            .Where(x => string.Equals(x.BusKey, identity.BusKey, StringComparison.Ordinal))
            .ToArray();

        if (registrations.Length > 1)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Transport Registration Bus", "unknown", $"Multiple payload-admission owners are registered for bus '{identity.BusKey}'.", "Correct the named configuration before starting the host"));

        if (registrations.Length == 1)
        {
            if (hostConfiguration is not IPayloadAdmissionHostConfiguration target)
                throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Transport Registration Bus", "unknown", $"Bus '{identity.BusKey}' does not support payload admission.", "Correct the named configuration before starting the host"));

            target.SetPayloadAdmissionRuntime(registrations[0].Runtime);
        }
    }

    static void ConnectReceiveEndpointObservers(IServiceProvider context, IReceiveEndpointObserverConnector connector)
    {
        foreach (var observer in context.GetServices<IReceiveEndpointObserver>())
            connector.ConnectReceiveEndpointObserver(observer);
    }

    static void ConnectReceiveObservers(IServiceProvider context, IReceiveObserverConnector connector)
    {
        foreach (var observer in context.GetServices<IReceiveObserver>())
            connector.ConnectReceiveObserver(observer);
    }

    static void ConnectConsumeObservers(IServiceProvider context, IConsumeObserverConnector connector)
    {
        foreach (var observer in context.GetServices<IConsumeObserver>())
            connector.ConnectConsumeObserver(observer);
    }

    static void ConnectSendObservers(IServiceProvider context, ISendObserverConnector connector)
    {
        foreach (var observer in context.GetServices<ISendObserver>())
            connector.ConnectSendObserver(observer);
    }

    static void ConnectPublishObservers(IServiceProvider context, IPublishObserverConnector connector)
    {
        foreach (var observer in context.GetServices<IPublishObserver>())
            connector.ConnectPublishObserver(observer);
    }

    /// <summary>Creates bus instance.</summary>
    /// <param name="bus">The bus.</param>
    /// <param name="host">The host.</param>
    /// <param name="hostConfiguration">The host configuration.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The created bus instance.</returns>
    protected virtual IBusInstance CreateBusInstance(IBusControl bus, IHost<TEndpointConfigurator> host, IHostConfiguration hostConfiguration,
        IBusRegistrationContext context)
    {
        return new TransportBusInstance<TEndpointConfigurator>(bus, host, hostConfiguration, context);
    }
}
