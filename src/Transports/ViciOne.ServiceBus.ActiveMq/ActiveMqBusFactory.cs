using System;
using ViciOne.ServiceBus.ActiveMq;
using ViciOne.ServiceBus.ActiveMq.Configuration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Creates ActiveMQ bus controls and their default message topology.</summary>
public static class ActiveMqBusFactory
{
    /// <summary>Creates an ActiveMQ bus control using the supplied transport configuration.</summary>
    /// <param name="configure">The callback that configures the ActiveMQ bus.</param>
    /// <returns>The configured bus control.</returns>
    public static IBusControl Create(Action<IActiveMqBusFactoryConfigurator> configure)
    {
        var topologyConfiguration = new ActiveMqTopologyConfiguration(CreateMessageTopology());
        var busConfiguration = new ActiveMqBusConfiguration(topologyConfiguration);

        var configurator = new ActiveMqBusFactoryConfigurator(busConfiguration);

        configure(configurator);

        return configurator.Build(busConfiguration);
    }

    /// <summary>Creates message topology that uses the ActiveMQ entity-name formatter.</summary>
    /// <returns>A new message-topology configurator.</returns>
    public static IMessageTopologyConfigurator CreateMessageTopology()
    {
        return new MessageTopology(Cached.EntityNameFormatter);
    }


    static class Cached
    {
        internal static readonly IEntityNameFormatter EntityNameFormatter;

        static Cached()
        {
            EntityNameFormatter = new MessageNameFormatterEntityNameFormatter(new ActiveMqMessageNameFormatter());
        }
    }
}
