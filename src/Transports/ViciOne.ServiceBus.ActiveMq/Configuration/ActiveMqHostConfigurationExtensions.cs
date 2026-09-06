using System;
using ViciOne.ServiceBus.ActiveMq.Configuration;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Configures ActiveMQ hosts and receive endpoints.</summary>
public static class ActiveMqHostConfigurationExtensions
{
    /// <summary>Configures an ActiveMQ host from an absolute broker address.</summary>
    /// <param name="configurator">The ActiveMQ bus configurator.</param>
    /// <param name="hostAddress">An <c>activemq</c> or <c>amqp</c> broker URI with an explicit port.</param>
    /// <param name="configure">The callback that configures credentials and native provider options.</param>
    public static void Host(this IActiveMqBusFactoryConfigurator configurator, Uri hostAddress, Action<IActiveMqHostConfigurator> configure)
    {
        if (hostAddress == null)
            throw new ArgumentNullException(nameof(hostAddress));

        var hostConfigurator = new ActiveMqHostConfigurator(hostAddress);

        configure(hostConfigurator);

        configurator.Host(hostConfigurator.Settings);
    }

    /// <summary>Configures an ActiveMQ host from an explicit protocol, host name, and port.</summary>
    /// <param name="configurator">The ActiveMQ bus configurator.</param>
    /// <param name="hostName">The host name of the broker.</param>
    /// <param name="protocol">The wire protocol used by the broker endpoint.</param>
    /// <param name="port">The port to connect to the broker.</param>
    /// <param name="configure">The callback that configures credentials and native provider options.</param>
    public static void Host(this IActiveMqBusFactoryConfigurator configurator, string hostName,
        ActiveMqTransportProtocol protocol, int port,
        Action<IActiveMqHostConfigurator> configure)
    {
        configurator.Host(new ActiveMqHostAddress(protocol, hostName, port, "/"), configure);
    }

    /// <summary>
    /// Adds a receive endpoint with a generated, non-durable, auto-delete queue name.
    /// </summary>
    /// <param name="configurator">The ActiveMQ bus configurator.</param>
    /// <param name="configure">An optional callback that configures the receive endpoint.</param>
    public static void ReceiveEndpoint(this IActiveMqBusFactoryConfigurator configurator,
        Action<IActiveMqReceiveEndpointConfigurator>? configure = null)
    {
        configurator.ReceiveEndpoint(new TemporaryEndpointDefinition(), DefaultEndpointNameFormatter.Instance, configure);
    }

    /// <summary>Adds a receive endpoint described by an endpoint definition.</summary>
    /// <param name="configurator">The ActiveMQ bus configurator.</param>
    /// <param name="definition">The endpoint definition.</param>
    /// <param name="configure">An optional callback that configures the receive endpoint.</param>
    public static void ReceiveEndpoint(this IActiveMqBusFactoryConfigurator configurator, IEndpointDefinition definition,
        Action<IActiveMqReceiveEndpointConfigurator>? configure = null)
    {
        configurator.ReceiveEndpoint(definition, DefaultEndpointNameFormatter.Instance, configure);
    }
}
