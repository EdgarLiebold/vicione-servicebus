using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>
/// Defines the contract for rabbit mq host configuration.
/// </summary>
public interface IRabbitMqHostConfiguration :
    IHostConfiguration,
    IReceiveConfigurator<IRabbitMqReceiveEndpointConfigurator>
{
    /// <summary>
    /// Gets the connection context supervisor value.
    /// </summary>
    IConnectionContextSupervisor ConnectionContextSupervisor { get; }

    /// <summary>
    /// Gets or sets the settings value.
    /// </summary>
    RabbitMqHostSettings Settings { get; set; }

    /// <summary>
    /// True if the broker is confirming published messages
    /// </summary>
    bool PublisherConfirmation { get; }

    /// <summary>
    /// Gets the batch settings value.
    /// </summary>
    BatchSettings BatchSettings { get; }

    /// <summary>
    /// Gets the topology value.
    /// </summary>
    new IRabbitMqBusTopology Topology { get; }

    /// <summary>
    /// Apply the endpoint definition to the receive endpoint configurator
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="definition"></param>
    void ApplyEndpointDefinition(IRabbitMqReceiveEndpointConfigurator configurator, IEndpointDefinition definition);

    /// <summary>
    /// Creates receive endpoint configuration.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    IRabbitMqReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(string queueName,
        Action<IRabbitMqReceiveEndpointConfigurator>? configure = null);

    /// <summary>
    /// Creates receive endpoint configuration.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    /// <param name="endpointConfiguration">The endpoint configuration value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    IRabbitMqReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(RabbitMqReceiveSettings settings,
        IRabbitMqEndpointConfiguration endpointConfiguration, Action<IRabbitMqReceiveEndpointConfigurator>? configure = null);
}
