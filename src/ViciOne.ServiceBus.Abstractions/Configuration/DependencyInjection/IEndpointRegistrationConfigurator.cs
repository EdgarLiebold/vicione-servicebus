using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures transport-independent settings for a registered receive endpoint.</summary>
public interface IEndpointRegistrationConfigurator
{
    /// <summary>Sets an explicit endpoint name instead of using the configured naming convention.</summary>
    string Name { set; }

    /// <summary>Sets whether the endpoint and its broker resources are removed when the endpoint stops.</summary>
    bool Temporary { set; }

    /// <summary>
    /// Sets the broker-specific number of messages fetched ahead of processing.
    /// </summary>
    int? PrefetchCount { set; }

    /// <summary>
    /// Sets the maximum number of messages processed concurrently on the endpoint.
    /// </summary>
    int? ConcurrentMessageLimit { set; }

    /// <summary>
    /// Sets whether the transport creates the consume topology required to route messages to the endpoint.
    /// </summary>
    bool ConfigureConsumeTopology { set; }

    /// <summary>
    /// Sets the endpoint-instance identifier appended to the endpoint name.
    /// </summary>
    string InstanceId { set; }

    /// <summary>Adds a callback that configures the transport-specific receive endpoint.</summary>
    /// <param name="callback">The callback invoked when the endpoint is configured.</param>
    void AddConfigureEndpointCallback(Action<IReceiveEndpointConfigurator> callback);

    /// <summary>Adds a callback that can resolve registered services while configuring the receive endpoint.</summary>
    /// <param name="callback">The callback invoked when the endpoint is configured.</param>
    void AddConfigureEndpointCallback(Action<IRegistrationContext, IReceiveEndpointConfigurator> callback);
}
