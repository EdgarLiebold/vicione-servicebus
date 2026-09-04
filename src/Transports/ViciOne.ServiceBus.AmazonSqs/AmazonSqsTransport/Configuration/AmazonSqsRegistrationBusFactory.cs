using System;
using System.Collections.Generic;
using Amazon;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs.Configuration;

/// <summary>
/// Provides an amazon sqs registration bus factory implementation.
/// </summary>
public class AmazonSqsRegistrationBusFactory :
    TransportRegistrationBusFactory<IAmazonSqsReceiveEndpointConfigurator>
{
    readonly AmazonSqsBusConfiguration _busConfiguration;
    readonly Action<IBusRegistrationContext, IAmazonSqsBusFactoryConfigurator>? _configure;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
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

    /// <summary>
    /// Creates bus.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="specifications">The specifications value.</param>
    /// <param name="busName">The bus name value.</param>
    /// <returns>The result of the operation.</returns>
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
