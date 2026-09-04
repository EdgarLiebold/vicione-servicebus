using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs.Configuration;

/// <summary>
/// Defines the contract for amazon sqs receive endpoint configuration.
/// </summary>
public interface IAmazonSqsReceiveEndpointConfiguration :
    IReceiveEndpointConfiguration,
    IAmazonSqsEndpointConfiguration
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
