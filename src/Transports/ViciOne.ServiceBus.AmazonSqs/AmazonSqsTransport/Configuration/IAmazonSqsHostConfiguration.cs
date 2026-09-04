using System;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.AmazonSqs.Configuration;

/// <summary>
/// Defines the contract for amazon sqs host configuration.
/// </summary>
public interface IAmazonSqsHostConfiguration :
    IHostConfiguration,
    IReceiveConfigurator<IAmazonSqsReceiveEndpointConfigurator>
{
    /// <summary>
    /// Gets or sets the settings value.
    /// </summary>
    AmazonSqsHostSettings Settings { get; set; }

    /// <summary>
    /// Gets the connection context supervisor value.
    /// </summary>
    IConnectionContextSupervisor ConnectionContextSupervisor { get; }

    /// <summary>
    /// Gets the topology value.
    /// </summary>
    new IAmazonSqsBusTopology Topology { get; }

    /// <summary>
    /// Apply the endpoint definition to the receive endpoint configurator
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="definition"></param>
    void ApplyEndpointDefinition(IAmazonSqsReceiveEndpointConfigurator configurator, IEndpointDefinition definition);

    /// <summary>
    /// Create a receive endpoint configuration using the specified host
    /// </summary>
    /// <returns></returns>
    IAmazonSqsReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(string queueName,
        Action<IAmazonSqsReceiveEndpointConfigurator>? configure = null);

    /// <summary>
    /// Create a receive endpoint configuration for the default host
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="endpointConfiguration"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
    IAmazonSqsReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(QueueReceiveSettings settings,
        IAmazonSqsEndpointConfiguration endpointConfiguration, Action<IAmazonSqsReceiveEndpointConfigurator>? configure = null);
}
