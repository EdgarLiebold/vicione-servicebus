using System;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.RabbitMq;
using ViciOne.ServiceBus.RabbitMq.Configuration;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Creates RabbitMQ-backed bus instances and their message topology.</summary>
public static class RabbitMqBusFactory
{
    /// <summary>Creates a RabbitMQ bus from an optional transport configuration callback.</summary>
    /// <param name="configure">An optional callback that configures the bus and its endpoints.</param>
    /// <returns>The configured bus control.</returns>
    public static IBusControl Create(Action<IRabbitMqBusFactoryConfigurator>? configure = null)
    {
        var topologyConfiguration = new RabbitMqTopologyConfiguration(CreateMessageTopology());
        var busConfiguration = new RabbitMqBusConfiguration(topologyConfiguration);

        var configurator = new RabbitMqBusFactoryConfigurator(busConfiguration);

        configure?.Invoke(configurator);

        return configurator.Build(busConfiguration);
    }

    /// <summary>Creates message topology that formats RabbitMQ exchange names.</summary>
    /// <returns>A new RabbitMQ message-topology configurator.</returns>
    public static IMessageTopologyConfigurator CreateMessageTopology()
    {
        return new MessageTopology(Cached.EntityNameFormatter);
    }


    static class Cached
    {
        internal static readonly IEntityNameFormatter EntityNameFormatter;

        static Cached()
        {
            EntityNameFormatter = new MessageNameFormatterEntityNameFormatter(new RabbitMqMessageNameFormatter());
        }
    }
}
