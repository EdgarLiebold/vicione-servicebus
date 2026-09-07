using System;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.InMemoryTransport.Configuration;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Creates standalone buses backed by the process-local in-memory transport.</summary>
public static class InMemoryBus
{
    /// <summary>Creates an in-memory bus at the default loopback address.</summary>
    /// <param name="configure">The callback that configures the bus before validation and construction.</param>
    /// <returns>The constructed bus control.</returns>
    public static IBusControl Create(Action<IInMemoryBusFactoryConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        return Create(null, configure);
    }

    /// <summary>Creates an in-memory bus at an optional custom loopback address.</summary>
    /// <param name="baseAddress">The transport base address, or <see langword="null" /> for the default.</param>
    /// <param name="configure">The callback that configures the bus before validation and construction.</param>
    /// <returns>The constructed bus control.</returns>
    public static IBusControl Create(Uri? baseAddress, Action<IInMemoryBusFactoryConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var topologyConfiguration = new InMemoryTopologyConfiguration(CreateMessageTopology());
        var busConfiguration = new InMemoryBusConfiguration(topologyConfiguration, baseAddress);

        var configurator = new InMemoryBusFactoryConfigurator(busConfiguration);

        configure(configurator);

        return configurator.Build(busConfiguration);
    }

    /// <summary>Creates an in-memory message topology using the transport's entity-name formatter.</summary>
    /// <returns>A new mutable message-topology configurator.</returns>
    public static IMessageTopologyConfigurator CreateMessageTopology()
    {
        return new MessageTopology(Cached.EntityNameFormatter);
    }
    static class Cached
    {
        internal static readonly IEntityNameFormatter EntityNameFormatter;

        static Cached()
        {
            EntityNameFormatter = new MessageUrnEntityNameFormatter();
        }
    }
}
