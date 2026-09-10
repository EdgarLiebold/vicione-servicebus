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
        return CreateCore(null, configure);
    }

    /// <summary>Creates an in-memory bus at a custom loopback address.</summary>
    /// <param name="baseAddress">The transport base address.</param>
    /// <param name="configure">The callback that configures the bus before validation and construction.</param>
    /// <returns>The constructed bus control.</returns>
    public static IBusControl Create(Uri baseAddress, Action<IInMemoryBusFactoryConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(baseAddress);
        ArgumentNullException.ThrowIfNull(configure);

        return CreateCore(baseAddress, configure);
    }

    static IBusControl CreateCore(Uri? baseAddress, Action<IInMemoryBusFactoryConfigurator> configure)
    {
        var topologyConfiguration = new InMemoryTopologyConfiguration(CreateMessageTopology());
        var busConfiguration = new InMemoryBusConfiguration(topologyConfiguration, baseAddress);

        var configurator = new InMemoryBusFactoryConfigurator(busConfiguration);

        configure(configurator);

        return configurator.Build(busConfiguration);
    }

    /// <summary>Creates the mutable message topology shared by in-memory transport configuration.</summary>
    /// <returns>A new mutable message-topology configurator.</returns>
    internal static IMessageTopologyConfigurator CreateMessageTopology()
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
