using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

/// <summary>Creates in memory registration bus instances.</summary>
public class InMemoryRegistrationBusFactory :
    TransportRegistrationBusFactory<IInMemoryReceiveEndpointConfigurator>
{
    readonly InMemoryBusConfiguration _busConfiguration;
    readonly Action<IBusRegistrationContext, IInMemoryBusFactoryConfigurator>? _configure = null!;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="baseAddress">The base address.</param>
    /// <param name="configure">The callback used to configure the component.</param>
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

    /// <summary>Creates bus.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="specifications">The specifications.</param>
    /// <param name="busName">The bus name.</param>
    /// <returns>The created bus.</returns>
    public override IBusInstance CreateBus(IBusRegistrationContext context, IEnumerable<IBusInstanceSpecification> specifications, string busName)
    {
        var configurator = new InMemoryBusFactoryConfigurator(_busConfiguration);

        return CreateBus(configurator, context, _configure, specifications);
    }

    /// <summary>Creates bus instance.</summary>
    /// <param name="bus">The bus.</param>
    /// <param name="host">The host.</param>
    /// <param name="hostConfiguration">The host configuration.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The created bus instance.</returns>
    protected override IBusInstance CreateBusInstance(IBusControl bus, IHost<IInMemoryReceiveEndpointConfigurator> host,
        IHostConfiguration hostConfiguration, IBusRegistrationContext context)
    {
        return new InMemoryBusInstance(bus, host, hostConfiguration, context);
    }
}
