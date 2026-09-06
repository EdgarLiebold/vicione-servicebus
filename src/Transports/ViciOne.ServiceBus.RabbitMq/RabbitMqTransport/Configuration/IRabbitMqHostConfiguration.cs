using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>Owns RabbitMQ connection supervision, topology, and receive-endpoint construction for a bus.</summary>
public interface IRabbitMqHostConfiguration :
    IHostConfiguration,
    IReceiveConfigurator<IRabbitMqReceiveEndpointConfigurator>
{
    /// <summary>Gets the supervisor that owns and recreates RabbitMQ connection contexts.</summary>
    IConnectionContextSupervisor ConnectionContextSupervisor { get; }

    /// <summary>Gets or sets the effective RabbitMQ connection settings.</summary>
    RabbitMqHostSettings Settings { get; set; }

    /// <summary>Gets whether RabbitMQ publisher confirmations are enabled.</summary>
    bool PublisherConfirmation { get; }

    /// <summary>Gets client-side publish-batch settings.</summary>
    BatchSettings BatchSettings { get; }

    /// <summary>Gets the RabbitMQ send and publish topology for this host.</summary>
    new IRabbitMqBusTopology Topology { get; }

    /// <summary>Applies provider-neutral endpoint settings to a RabbitMQ receive endpoint.</summary>
    /// <param name="configurator">The RabbitMQ receive-endpoint configurator.</param>
    /// <param name="definition">The provider-neutral endpoint definition.</param>
    void ApplyEndpointDefinition(IRabbitMqReceiveEndpointConfigurator configurator, IEndpointDefinition definition);

    /// <summary>Creates receive endpoint configuration.</summary>
    /// <param name="queueName">The receive queue name.</param>
    /// <param name="configure">An optional callback that customizes the endpoint.</param>
    /// <returns>The new RabbitMQ receive-endpoint configuration.</returns>
    IRabbitMqReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(string queueName,
        Action<IRabbitMqReceiveEndpointConfigurator>? configure = null);

    /// <summary>Creates receive endpoint configuration.</summary>
    /// <param name="settings">The RabbitMQ receive and queue settings.</param>
    /// <param name="endpointConfiguration">The provider-neutral endpoint configuration to compose.</param>
    /// <param name="configure">An optional callback that customizes the endpoint.</param>
    /// <returns>The new RabbitMQ receive-endpoint configuration.</returns>
    IRabbitMqReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(RabbitMqReceiveSettings settings,
        IRabbitMqEndpointConfiguration endpointConfiguration, Action<IRabbitMqReceiveEndpointConfigurator>? configure = null);
}
