using System;
using ViciOne.ServiceBus.RabbitMq.Configuration;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Applies RabbitMQ host settings and creates receive endpoints.</summary>
public static class RabbitMqHostConfigurationExtensions
{
    /// <summary>Configures the RabbitMQ host from a transport URI.</summary>
    /// <param name="configurator">The RabbitMQ bus factory configurator.</param>
    /// <param name="hostAddress">The URI host address of the RabbitMQ host (rabbitmq://host:port/vhost).</param>
    /// <param name="configure">An optional callback that customizes connection behavior.</param>
    public static void Host(this IRabbitMqBusFactoryConfigurator configurator, Uri hostAddress,
        Action<IRabbitMqHostConfigurator>? configure = null)
    {
        configurator.Host(hostAddress, null, configure);
    }

    /// <summary>Configures the RabbitMQ host from a host name or absolute transport URI.</summary>
    /// <param name="configurator">The RabbitMQ bus factory configurator.</param>
    /// <param name="host">The host name of the broker, or a well-formed URI host address.</param>
    /// <param name="configure">The configuration callback.</param>
    public static void Host(this IRabbitMqBusFactoryConfigurator configurator, string host,
        Action<IRabbitMqHostConfigurator>? configure = null)
    {
        if (Uri.IsWellFormedUriString(host, UriKind.Absolute))
            configurator.Host(new Uri(host), null, configure);
        else
            configurator.Host(host, "/", null, configure);
    }

    /// <summary>Configures the RabbitMQ host and client-provided connection name from a transport URI.</summary>
    /// <param name="configurator">The RabbitMQ bus factory configurator.</param>
    /// <param name="hostAddress">The URI host address of the RabbitMQ host (rabbitmq://host:port/vhost).</param>
    /// <param name="connectionName">The client-provided connection name.</param>
    /// <param name="configure">An optional callback that customizes connection behavior.</param>
    public static void Host(this IRabbitMqBusFactoryConfigurator configurator, Uri hostAddress, string? connectionName,
        Action<IRabbitMqHostConfigurator>? configure = null)
    {
        var hostConfigurator = new RabbitMqHostConfigurator(hostAddress, connectionName);

        configure?.Invoke(hostConfigurator);

        configurator.Host(hostConfigurator.Settings);
    }

    /// <summary>Configures a RabbitMQ host name and virtual host.</summary>
    /// <param name="configurator">The RabbitMQ bus factory configurator.</param>
    /// <param name="host">The host name of the broker.</param>
    /// <param name="virtualHost">The virtual host to use.</param>
    /// <param name="configure">The configuration callback.</param>
    public static void Host(this IRabbitMqBusFactoryConfigurator configurator, string host, string virtualHost,
        Action<IRabbitMqHostConfigurator>? configure = null)
    {
        configurator.Host(host, virtualHost, null, configure);
    }

    /// <summary>Configures a RabbitMQ host name, virtual host, and client-provided connection name.</summary>
    /// <param name="configurator">The RabbitMQ bus factory configurator.</param>
    /// <param name="host">The host name of the broker.</param>
    /// <param name="virtualHost">The virtual host to use.</param>
    /// <param name="connectionName">The client-provided connection name.</param>
    /// <param name="configure">The configuration callback.</param>
    public static void Host(this IRabbitMqBusFactoryConfigurator configurator, string host, string virtualHost, string? connectionName,
        Action<IRabbitMqHostConfigurator>? configure = null)
    {
        if (host == null)
            throw new ArgumentNullException(nameof(host));
        if (virtualHost == null)
            throw new ArgumentNullException(nameof(virtualHost));

        var hostConfigurator = new RabbitMqHostConfigurator(host, virtualHost, connectionName: connectionName);

        configure?.Invoke(hostConfigurator);

        configurator.Host(hostConfigurator.Settings);
    }

    /// <summary>Configures an explicit RabbitMQ host, port, and virtual host.</summary>
    /// <param name="configurator">The RabbitMQ bus factory configurator.</param>
    /// <param name="host">The host name of the broker.</param>
    /// <param name="port">The port to connect to the broker.</param>
    /// <param name="virtualHost">The virtual host to use.</param>
    /// <param name="configure">The configuration callback.</param>
    public static void Host(this IRabbitMqBusFactoryConfigurator configurator, string host, ushort port, string virtualHost,
        Action<IRabbitMqHostConfigurator> configure)
    {
        configurator.Host(host, port, virtualHost, null, configure);
    }

    /// <summary>Configures an explicit RabbitMQ host, port, virtual host, and client-provided connection name.</summary>
    /// <param name="configurator">The RabbitMQ bus factory configurator.</param>
    /// <param name="host">The host name of the broker.</param>
    /// <param name="port">The port to connect to the broker.</param>
    /// <param name="virtualHost">The virtual host to use.</param>
    /// <param name="connectionName">The client-provided connection name.</param>
    /// <param name="configure">The configuration callback.</param>
    public static void Host(this IRabbitMqBusFactoryConfigurator configurator, string host, ushort port, string virtualHost,
        string? connectionName, Action<IRabbitMqHostConfigurator>? configure = null)
    {
        if (host == null)
            throw new ArgumentNullException(nameof(host));
        if (virtualHost == null)
            throw new ArgumentNullException(nameof(virtualHost));

        var hostConfigurator = new RabbitMqHostConfigurator(host, virtualHost, port, connectionName);

        configure?.Invoke(hostConfigurator);

        configurator.Host(hostConfigurator.Settings);
    }

    /// <summary>
    /// Declares a temporary receive endpoint with a generated, non-durable, auto-delete queue name.
    /// </summary>
    /// <param name="configurator">The RabbitMQ bus factory configurator.</param>
    /// <param name="configure">An optional endpoint configuration callback.</param>
    public static void ReceiveEndpoint(this IRabbitMqBusFactoryConfigurator configurator, Action<IRabbitMqReceiveEndpointConfigurator>? configure = null)
    {
        configurator.ReceiveEndpoint(new TemporaryEndpointDefinition(), DefaultEndpointNameFormatter.Instance, configure);
    }

    /// <summary>Declares a receive endpoint from an endpoint definition.</summary>
    /// <param name="configurator">The RabbitMQ bus factory configurator.</param>
    /// <param name="definition">The endpoint settings and identity.</param>
    /// <param name="configure">An optional endpoint configuration callback.</param>
    public static void ReceiveEndpoint(this IRabbitMqBusFactoryConfigurator configurator, IEndpointDefinition definition,
        Action<IRabbitMqReceiveEndpointConfigurator>? configure = null)
    {
        configurator.ReceiveEndpoint(definition, DefaultEndpointNameFormatter.Instance, configure);
    }
}
