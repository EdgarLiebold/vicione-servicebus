using System;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.RabbitMq;
using ViciOne.ServiceBus.RabbitMq.Configuration;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Provides a rabbit mq bus factory implementation.
/// </summary>
public static class RabbitMqBusFactory
{
    /// <summary>
    /// Configure and create a bus for RabbitMQ
    /// </summary>
    /// <param name="configure">The configuration callback to configure the bus</param>
    /// <returns></returns>
    public static IBusControl Create(Action<IRabbitMqBusFactoryConfigurator>? configure = null)
    {
        var topologyConfiguration = new RabbitMqTopologyConfiguration(CreateMessageTopology());
        var busConfiguration = new RabbitMqBusConfiguration(topologyConfiguration);

        var configurator = new RabbitMqBusFactoryConfigurator(busConfiguration);

        configure?.Invoke(configurator);

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
            EntityNameFormatter = new MessageNameFormatterEntityNameFormatter(new RabbitMqMessageNameFormatter());
        }
    }
}
