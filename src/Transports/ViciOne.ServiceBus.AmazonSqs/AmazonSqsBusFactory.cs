using System;
using ViciOne.ServiceBus.AmazonSqs;
using ViciOne.ServiceBus.AmazonSqs.Configuration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Creates Amazon SQS bus controls and their default message topology.</summary>
public static class AmazonSqsBusFactory
{
    /// <summary>Creates an Amazon SQS bus control using the supplied transport configuration.</summary>
    /// <param name="configure">The callback that configures the Amazon SQS bus.</param>
    /// <returns>The configured bus control.</returns>
    public static IBusControl Create(Action<IAmazonSqsBusFactoryConfigurator> configure)
    {
        var topologyConfiguration = new AmazonSqsTopologyConfiguration(CreateMessageTopology());
        var busConfiguration = new AmazonSqsBusConfiguration(topologyConfiguration);

        var configurator = new AmazonSqsBusFactoryConfigurator(busConfiguration);

        configure(configurator);

        return configurator.Build(busConfiguration);
    }

    /// <summary>Creates message topology that uses the Amazon entity-name formatter.</summary>
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
            EntityNameFormatter = new MessageNameFormatterEntityNameFormatter(new AmazonSqsMessageNameFormatter());
        }
    }
}
