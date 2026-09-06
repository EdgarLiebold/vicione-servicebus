using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMq.Configuration;

/// <summary>Creates dependency-injection-registered ActiveMQ bus instances.</summary>
public class ActiveMqRegistrationBusFactory :
    TransportRegistrationBusFactory<IActiveMqReceiveEndpointConfigurator>
{
    readonly ActiveMqBusConfiguration _busConfiguration;
    readonly Action<IBusRegistrationContext, IActiveMqBusFactoryConfigurator>? _configure;

    /// <summary>Creates a registration bus factory with an optional bus callback.</summary>
    /// <param name="configure">An optional callback that configures the bus using its registration context.</param>
    public ActiveMqRegistrationBusFactory(Action<IBusRegistrationContext, IActiveMqBusFactoryConfigurator>? configure)
        : this(new ActiveMqBusConfiguration(new ActiveMqTopologyConfiguration(ActiveMqBusFactory.CreateMessageTopology())), configure)
    {
    }

    ActiveMqRegistrationBusFactory(ActiveMqBusConfiguration busConfiguration,
        Action<IBusRegistrationContext, IActiveMqBusFactoryConfigurator>? configure)
        : base(busConfiguration.HostConfiguration)
    {
        _configure = configure;

        _busConfiguration = busConfiguration;
    }

    /// <summary>Creates a registered ActiveMQ bus using named transport options and registration specifications.</summary>
    /// <param name="context">The dependency-injection bus registration context.</param>
    /// <param name="specifications">The specifications applied to the bus.</param>
    /// <param name="busName">The name used to select transport options.</param>
    /// <returns>The created bus instance.</returns>
    public override IBusInstance CreateBus(IBusRegistrationContext context, IEnumerable<IBusInstanceSpecification> specifications, string busName)
    {
        var configurator = new ActiveMqBusFactoryConfigurator(_busConfiguration);

        var options = context.GetRequiredService<IOptionsMonitor<ActiveMqTransportOptions>>().Get(busName);
        Uri? hostAddress = GetHostAddress(options);

        if (hostAddress != null)
        {
            configurator.Host(hostAddress, h =>
            {
                if (!string.IsNullOrWhiteSpace(options.User))
                    h.Username(options.User);
                if (!string.IsNullOrWhiteSpace(options.Pass))
                    h.Password(options.Pass);

                if (options.UseSsl)
                    h.UseSsl();
            });
        }

        return CreateBus(configurator, context, _configure, specifications);
    }

    internal static Uri? GetHostAddress(ActiveMqTransportOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var hasHost = !string.IsNullOrWhiteSpace(options.Host);
        var hasAnyConfiguration = hasHost
            || options.Protocol.HasValue
            || options.Port.HasValue
            || options.UseSsl
            || !string.IsNullOrWhiteSpace(options.User)
            || !string.IsNullOrWhiteSpace(options.Pass);

        if (!hasAnyConfiguration)
            return null;

        if (!hasHost || options.Host == null)
            throw new ActiveMqTransportConfigurationException("The ActiveMQ host must be configured when transport options are present.");
        if (!options.Protocol.HasValue)
            throw new ActiveMqTransportConfigurationException("The ActiveMQ protocol must be configured explicitly.");
        if (!options.Port.HasValue || options.Port.Value == 0)
            throw new ActiveMqTransportConfigurationException("The ActiveMQ port must be configured explicitly and must be between 1 and 65535.");

        var scheme = options.Protocol.Value switch
        {
            ActiveMqTransportProtocol.OpenWire => ActiveMqHostAddress.ActiveMqScheme,
            ActiveMqTransportProtocol.Amqp => ActiveMqHostAddress.AmqpScheme,
            _ => throw new ActiveMqTransportConfigurationException($"The ActiveMQ protocol is not supported: {options.Protocol.Value}")
        };

        try
        {
            return new UriBuilder
            {
                Scheme = scheme,
                Host = options.Host,
                Port = options.Port.Value,
                Path = "/"
            }.Uri;
        }
        catch (UriFormatException exception)
        {
            throw new ActiveMqTransportConfigurationException($"The ActiveMQ host is invalid: {options.Host}", exception);
        }
    }
}
