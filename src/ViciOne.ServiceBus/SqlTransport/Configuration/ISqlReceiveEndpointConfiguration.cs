using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>
/// Defines the contract for sql receive endpoint configuration.
/// </summary>
public interface ISqlReceiveEndpointConfiguration :
    IReceiveEndpointConfiguration,
    ISqlEndpointConfiguration
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
