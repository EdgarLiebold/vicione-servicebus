using System;
using System.Collections.Generic;
using Amazon;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs.Configuration;

/// <summary>Creates Amazon SQS bus instances from dependency-injection registrations and named transport options.</summary>
public class AmazonSqsRegistrationBusFactory :
    TransportRegistrationBusFactory<IAmazonSqsReceiveEndpointConfigurator>
{
    readonly AmazonSqsBusConfiguration _busConfiguration;
    readonly Action<IBusRegistrationContext, IAmazonSqsBusFactoryConfigurator>? _configure;

    /// <summary>Initializes a registration-based Amazon SQS bus factory.</summary>
    /// <param name="configure">An optional callback that configures each created bus.</param>
    public AmazonSqsRegistrationBusFactory(Action<IBusRegistrationContext, IAmazonSqsBusFactoryConfigurator>? configure)
        : this(new AmazonSqsBusConfiguration(new AmazonSqsTopologyConfiguration(AmazonSqsBusFactory.CreateMessageTopology())), configure)
    {
    }

    AmazonSqsRegistrationBusFactory(AmazonSqsBusConfiguration busConfiguration,
        Action<IBusRegistrationContext, IAmazonSqsBusFactoryConfigurator>? configure)
        : base(busConfiguration.HostConfiguration)
    {
        _configure = configure;

        _busConfiguration = busConfiguration;
    }

    /// <summary>Creates a bus using its named region and scope options, registrations, and endpoint specifications.</summary>
    /// <param name="context">The bus registration context.</param>
    /// <param name="specifications">The specifications applied to the bus instance.</param>
    /// <param name="busName">The name used to resolve transport options.</param>
    /// <returns>The configured bus instance.</returns>
    public override IBusInstance CreateBus(IBusRegistrationContext context, IEnumerable<IBusInstanceSpecification> specifications, string busName)
    {
        var configurator = new AmazonSqsBusFactoryConfigurator(_busConfiguration);

        var options = context.GetRequiredService<IOptionsMonitor<AmazonSqsTransportOptions>>().Get(busName);
        if (!string.IsNullOrWhiteSpace(options.Region))
        {
            var regionEndpoint = RegionEndpoint.GetBySystemName(options.Region);

            configurator.Host(regionEndpoint.SystemName, h =>
            {
                if (!string.IsNullOrWhiteSpace(options.Scope))
                    h.Scope(options.Scope!);

            });
        }

        return CreateBus(configurator, context, _configure, specifications);
    }
}
