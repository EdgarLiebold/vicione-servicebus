using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.AzureServiceBus.Configuration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Provides a service bus registration bus factory implementation.
/// </summary>
public class ServiceBusRegistrationBusFactory :
    TransportRegistrationBusFactory<IServiceBusReceiveEndpointConfigurator>
{
    readonly ServiceBusBusConfiguration _busConfiguration;
    readonly Action<IBusRegistrationContext, IServiceBusBusFactoryConfigurator>? _configure;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
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

    /// <summary>
    /// Creates bus.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="specifications">The specifications value.</param>
    /// <param name="busName">The bus name value.</param>
    /// <returns>The result of the operation.</returns>
    public override IBusInstance CreateBus(IBusRegistrationContext context, IEnumerable<IBusInstanceSpecification> specifications, string busName)
    {
        var configurator = new ServiceBusBusFactoryConfigurator(_busConfiguration);

        var options = context.GetRequiredService<IOptionsMonitor<AzureServiceBusTransportOptions>>().Get(busName);
        if (!string.IsNullOrWhiteSpace(options.ConnectionString))
            configurator.Host(options.ConnectionString);

        return CreateBus(configurator, context, _configure, specifications);
    }

    /// <summary>
    /// Creates bus instance.
    /// </summary>
    /// <param name="bus">The bus value.</param>
    /// <param name="host">The host value.</param>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    protected override IBusInstance CreateBusInstance(IBusControl bus, IHost<IServiceBusReceiveEndpointConfigurator> host,
        IHostConfiguration hostConfiguration, IBusRegistrationContext context)
    {
        return new ServiceBusInstance(bus, host, hostConfiguration, context);
    }
}
