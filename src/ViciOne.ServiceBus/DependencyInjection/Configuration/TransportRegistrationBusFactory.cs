using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

public abstract class TransportRegistrationBusFactory<TEndpointConfigurator> :
    IRegistrationBusFactory
    where TEndpointConfigurator : class, IReceiveEndpointConfigurator
{
    readonly IHostConfiguration _hostConfiguration;

    protected TransportRegistrationBusFactory(IHostConfiguration hostConfiguration)
    {
        _hostConfiguration = hostConfiguration;
    }

    public abstract IBusInstance CreateBus(IBusRegistrationContext context, IEnumerable<IBusInstanceSpecification> specifications, string busName);

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

        ConfigurePayloadAdmission(context, _hostConfiguration);
        ConnectBusObservers(context, configurator);

        configure?.Invoke(context, configurator);

        IBusInstanceSpecification[] busInstanceSpecifications = specifications?.ToArray() ?? [];

        IEnumerable<ValidationResult> validationResult = configurator.Validate()
            .Concat(busInstanceSpecifications.SelectMany(x => x.Validate()));

        if (_hostConfiguration.BusConfiguration.MessageRoutes is not MessageRouteTable messageRoutes)
            throw new ConfigurationException("The bus must own a MessageRouteTable instance.");

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

            var host = _hostConfiguration.Build() as IHost<TEndpointConfigurator>;

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

            throw new ConfigurationException(result, "An exception occurred during bus creation", ex);
        }
    }

    static void ConnectBusObservers(IServiceProvider context, IBusObserverConnector connector)
    {
        foreach (var observer in context.GetServices<IBusObserver>())
            connector.ConnectBusObserver(observer);
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
            throw new ConfigurationException($"Multiple payload-admission owners are registered for bus '{identity.BusKey}'.");

        if (registrations.Length == 1)
        {
            if (hostConfiguration is not IPayloadAdmissionHostConfiguration target)
                throw new ConfigurationException($"Bus '{identity.BusKey}' does not support payload admission.");

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

    protected virtual IBusInstance CreateBusInstance(IBusControl bus, IHost<TEndpointConfigurator> host, IHostConfiguration hostConfiguration,
        IBusRegistrationContext context)
    {
        return new TransportBusInstance<TEndpointConfigurator>(bus, host, hostConfiguration, context);
    }
}
