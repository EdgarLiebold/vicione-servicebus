using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

/// <summary>Creates dependency-injection bus instances backed by the in-memory transport.</summary>
internal sealed class InMemoryRegistrationBusFactory :
    TransportRegistrationBusFactory<IInMemoryReceiveEndpointConfigurator>
{
    readonly InMemoryBusConfiguration _busConfiguration;
    readonly Action<IBusRegistrationContext, IInMemoryBusFactoryConfigurator>? _configure;

    /// <summary>Creates a registration factory for an optional loopback host address.</summary>
    /// <param name="baseAddress">The host address, or <see langword="null" /> for the default.</param>
    /// <param name="configure">An optional callback applied during bus construction.</param>
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

    /// <summary>Creates the configured bus for a registration scope.</summary>
    /// <param name="context">The registration context.</param>
    /// <param name="specifications">The bus-instance specifications to apply.</param>
    /// <param name="busName">The registered bus name, or an empty string for the default bus.</param>
    /// <returns>The created bus instance.</returns>
    public override IBusInstance CreateBus(IBusRegistrationContext context, IEnumerable<IBusInstanceSpecification> specifications, string busName)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(specifications);
        ArgumentNullException.ThrowIfNull(busName);
        var configurator = new InMemoryBusFactoryConfigurator(_busConfiguration);

        return CreateBus(configurator, context, _configure, specifications);
    }

    /// <summary>Wraps a constructed bus and host in the registered bus-instance contract.</summary>
    /// <param name="bus">The constructed bus control.</param>
    /// <param name="host">The in-memory host.</param>
    /// <param name="hostConfiguration">The host configuration.</param>
    /// <param name="context">The registration context.</param>
    /// <returns>The registered bus instance.</returns>
    protected override IBusInstance CreateBusInstance(IBusControl bus, IHost<IInMemoryReceiveEndpointConfigurator> host,
        IHostConfiguration hostConfiguration, IBusRegistrationContext context)
    {
        return new InMemoryBusInstance(bus, host, hostConfiguration, context);
    }
}
