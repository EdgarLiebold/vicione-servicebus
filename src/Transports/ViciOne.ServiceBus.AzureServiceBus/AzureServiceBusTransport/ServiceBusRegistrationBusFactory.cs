using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.AzureServiceBus.Configuration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Creates Azure Service Bus instances from dependency-injection registrations.</summary>
public class ServiceBusRegistrationBusFactory :
    TransportRegistrationBusFactory<IServiceBusReceiveEndpointConfigurator>
{
    readonly ServiceBusBusConfiguration _busConfiguration;
    readonly Action<IBusRegistrationContext, IServiceBusBusFactoryConfigurator>? _configure;

    /// <summary>Creates a factory with the default Azure Service Bus topology.</summary>
    /// <param name="configure">Optionally configures the bus with access to registration services.</param>
    public ServiceBusRegistrationBusFactory(Action<IBusRegistrationContext, IServiceBusBusFactoryConfigurator>? configure)
        : this(new ServiceBusBusConfiguration(new ServiceBusTopologyConfiguration(AzureBusFactory.CreateMessageTopology())), configure)
    {
    }

    ServiceBusRegistrationBusFactory(ServiceBusBusConfiguration busConfiguration,
        Action<IBusRegistrationContext, IServiceBusBusFactoryConfigurator>? configure)
        : base(busConfiguration.HostConfiguration)
    {
        _configure = configure;

        _busConfiguration = busConfiguration;
    }

    /// <summary>Creates a bus using named transport options and registered specifications.</summary>
    /// <param name="context">The bus registration context.</param>
    /// <param name="specifications">The endpoint and bus specifications to apply.</param>
    /// <param name="busName">The name used to resolve transport options.</param>
    /// <returns>The configured bus instance.</returns>
    public override IBusInstance CreateBus(IBusRegistrationContext context, IEnumerable<IBusInstanceSpecification> specifications, string busName)
    {
        var configurator = new ServiceBusBusFactoryConfigurator(_busConfiguration);

        var options = context.GetRequiredService<IOptionsMonitor<AzureServiceBusTransportOptions>>().Get(busName);
        if (!string.IsNullOrWhiteSpace(options.ConnectionString))
            configurator.Host(options.ConnectionString);

        return CreateBus(configurator, context, _configure, specifications);
    }

    /// <summary>Wraps a configured Azure Service Bus control and host as a running bus instance.</summary>
    /// <param name="bus">The configured bus control.</param>
    /// <param name="host">The Azure Service Bus host.</param>
    /// <param name="hostConfiguration">The active host configuration.</param>
    /// <param name="context">The bus registration context.</param>
    /// <returns>The Azure Service Bus bus instance.</returns>
    protected override IBusInstance CreateBusInstance(IBusControl bus, IHost<IServiceBusReceiveEndpointConfigurator> host,
        IHostConfiguration hostConfiguration, IBusRegistrationContext context)
    {
        return new ServiceBusInstance(bus, host, hostConfiguration, context);
    }
}
