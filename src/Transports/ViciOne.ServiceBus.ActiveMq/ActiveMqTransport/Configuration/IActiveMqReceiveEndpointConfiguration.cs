using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMq.Configuration;

/// <summary>
/// Defines the contract for active mq receive endpoint configuration.
/// </summary>
public interface IActiveMqReceiveEndpointConfiguration :
    IReceiveEndpointConfiguration,
    IActiveMqEndpointConfiguration
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
