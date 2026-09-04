using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>
/// Defines the contract for rabbit mq receive endpoint configuration.
/// </summary>
public interface IRabbitMqReceiveEndpointConfiguration :
    IReceiveEndpointConfiguration,
    IRabbitMqEndpointConfiguration
{
    /// <summary>
    /// Gets the settings value.
    /// </summary>
    ReceiveSettings Settings { get; }

    /// <summary>
    /// Performs the build operation.
    /// </summary>
    /// <param name="host">The host value.</param>
    void Build(IHost host);
}
