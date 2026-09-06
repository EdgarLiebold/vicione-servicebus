using System;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.InMemoryTransport.Configuration;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Provides the bus implementation for in memory.</summary>
public static class InMemoryBus
{
    /// <summary>Configure and create an in-memory bus.</summary>
    /// <param name="configure">The configuration callback to configure the bus.</param>
    /// <returns>The newly created instance.</returns>
    public static IBusControl Create(Action<IInMemoryBusFactoryConfigurator> configure)
    {
        return Create(null, configure);
    }

    /// <summary>Configure and create an in-memory bus.</summary>
    /// <param name="baseAddress">Override the default base address.</param>
    /// <param name="configure">The configuration callback to configure the bus.</param>
    /// <returns>The newly created instance.</returns>
    public static IBusControl Create(Uri? baseAddress, Action<IInMemoryBusFactoryConfigurator> configure)
    {
        var topologyConfiguration = new InMemoryTopologyConfiguration(CreateMessageTopology());
        var busConfiguration = new InMemoryBusConfiguration(topologyConfiguration, baseAddress);

        var configurator = new InMemoryBusFactoryConfigurator(busConfiguration);

        configure(configurator);

        return configurator.Build(busConfiguration);
    }

    /// <summary>Creates message topology.</summary>
    /// <returns>The created message topology.</returns>
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
