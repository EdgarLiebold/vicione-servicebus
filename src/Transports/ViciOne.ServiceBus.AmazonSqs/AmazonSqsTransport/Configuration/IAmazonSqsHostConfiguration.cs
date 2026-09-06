using System;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.AmazonSqs.Configuration;

/// <summary>Defines host settings, topology, connection supervision, and receive-endpoint creation for Amazon SQS.</summary>
public interface IAmazonSqsHostConfiguration :
    IHostConfiguration,
    IReceiveConfigurator<IAmazonSqsReceiveEndpointConfigurator>
{
    /// <summary>Gets or sets the immutable Amazon SQS host settings.</summary>
    AmazonSqsHostSettings Settings { get; set; }

    /// <summary>Gets the supervisor for Amazon SQS connection contexts.</summary>
    IConnectionContextSupervisor ConnectionContextSupervisor { get; }

    /// <summary>Gets the Amazon SQS bus topology.</summary>
    new IAmazonSqsBusTopology Topology { get; }

    /// <summary>Applies transport-neutral endpoint-definition settings to an Amazon SQS endpoint.</summary>
    /// <param name="configurator">The receive-endpoint configurator to update.</param>
    /// <param name="definition">The endpoint definition to apply.</param>
    void ApplyEndpointDefinition(IAmazonSqsReceiveEndpointConfigurator configurator, IEndpointDefinition definition);

    /// <summary>Creates receive-endpoint configuration for a named Amazon SQS queue.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="configure">An optional Amazon SQS-specific endpoint callback.</param>
    /// <returns>The configured receive endpoint.</returns>
    IAmazonSqsReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(string queueName,
        Action<IAmazonSqsReceiveEndpointConfigurator>? configure = null);

    /// <summary>Creates receive-endpoint configuration from prepared queue and endpoint settings.</summary>
    /// <param name="settings">The queue and receive settings.</param>
    /// <param name="endpointConfiguration">The endpoint-level pipeline and topology configuration.</param>
    /// <param name="configure">An optional Amazon SQS-specific endpoint callback.</param>
    /// <returns>The configured receive endpoint.</returns>
    IAmazonSqsReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(QueueReceiveSettings settings,
        IAmazonSqsEndpointConfiguration endpointConfiguration, Action<IAmazonSqsReceiveEndpointConfigurator>? configure = null);
}
