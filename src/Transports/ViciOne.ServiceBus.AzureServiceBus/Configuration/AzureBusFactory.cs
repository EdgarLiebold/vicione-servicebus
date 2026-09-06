using System;
using ViciOne.ServiceBus.AzureServiceBus;
using ViciOne.ServiceBus.AzureServiceBus.Configuration;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides an azure bus factory implementation.
/// </summary>
public static class AzureBusFactory
{
    /// <summary>
    /// Creates an Azure Service Bus instance using the supplied transport configuration.
    /// </summary>
    /// <param name="configure">The configuration callback to configure the bus</param>
    /// <returns>The configured bus.</returns>
    public static IBusControl CreateUsingServiceBus(Action<IServiceBusBusFactoryConfigurator> configure)
    {
        var topologyConfiguration = new ServiceBusTopologyConfiguration(CreateMessageTopology());
        var busConfiguration = new ServiceBusBusConfiguration(topologyConfiguration);

        var configurator = new ServiceBusBusFactoryConfigurator(busConfiguration);

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
            EntityNameFormatter = new MessageNameFormatterEntityNameFormatter(new ServiceBusMessageNameFormatter());
        }
    }
}
