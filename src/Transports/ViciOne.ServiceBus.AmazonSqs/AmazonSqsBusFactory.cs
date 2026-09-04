using System;
using ViciOne.ServiceBus.AmazonSqs;
using ViciOne.ServiceBus.AmazonSqs.Configuration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Provides an amazon sqs bus factory implementation.
/// </summary>
public static class AmazonSqsBusFactory
{
    /// <summary>
    /// Configure and create a bus for AmazonSQS
    /// </summary>
    /// <param name="configure">The configuration callback to configure the bus</param>
    /// <returns></returns>
    public static IBusControl Create(Action<IAmazonSqsBusFactoryConfigurator> configure)
    {
        var topologyConfiguration = new AmazonSqsTopologyConfiguration(CreateMessageTopology());
        var busConfiguration = new AmazonSqsBusConfiguration(topologyConfiguration);

        var configurator = new AmazonSqsBusFactoryConfigurator(busConfiguration);

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
            EntityNameFormatter = new MessageNameFormatterEntityNameFormatter(new AmazonSqsMessageNameFormatter());
        }
    }
}
