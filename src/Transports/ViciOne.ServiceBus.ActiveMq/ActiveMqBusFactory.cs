using System;
using ViciOne.ServiceBus.ActiveMq;
using ViciOne.ServiceBus.ActiveMq.Configuration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Provides an active mq bus factory implementation.
/// </summary>
public static class ActiveMqBusFactory
{
    /// <summary>
    /// Configure and create a bus for ActiveMQ
    /// </summary>
    /// <param name="configure">The configuration callback to configure the bus</param>
    /// <returns></returns>
    public static IBusControl Create(Action<IActiveMqBusFactoryConfigurator> configure)
    {
        var topologyConfiguration = new ActiveMqTopologyConfiguration(CreateMessageTopology());
        var busConfiguration = new ActiveMqBusConfiguration(topologyConfiguration);

        var configurator = new ActiveMqBusFactoryConfigurator(busConfiguration);

        configure(configurator);

        return configurator.Build(busConfiguration);
    }

    /// <summary>
    /// Creates message topology.
    /// </summary>
    /// <returns>The result of the operation.</returns>
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
