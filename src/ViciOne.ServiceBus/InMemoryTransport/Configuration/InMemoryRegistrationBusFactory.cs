using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

/// <summary>
/// Provides an in memory registration bus factory implementation.
/// </summary>
public class InMemoryRegistrationBusFactory :
    TransportRegistrationBusFactory<IInMemoryReceiveEndpointConfigurator>
{
    readonly InMemoryBusConfiguration _busConfiguration;
    readonly Action<IBusRegistrationContext, IInMemoryBusFactoryConfigurator>? _configure = null!;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="baseAddress">The base address value.</param>
    /// <param name="configure">The configuration callback.</param>
    public InMemoryRegistrationBusFactory(Uri? baseAddress, Action<IBusRegistrationContext, IInMemoryBusFactoryConfigurator>? configure)
        : this(new InMemoryBusConfiguration(new InMemoryTopologyConfiguration(InMemoryBus.CreateMessageTopology()), baseAddress), configure)
    {
    }

    InMemoryRegistrationBusFactory(InMemoryBusConfiguration busConfiguration,
        Action<IBusRegistrationContext, IInMemoryBusFactoryConfigurator>? configure)
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
        var configurator = new InMemoryBusFactoryConfigurator(_busConfiguration);

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
    protected override IBusInstance CreateBusInstance(IBusControl bus, IHost<IInMemoryReceiveEndpointConfigurator> host,
        IHostConfiguration hostConfiguration, IBusRegistrationContext context)
    {
        return new InMemoryBusInstance(bus, host, hostConfiguration, context);
    }
}
