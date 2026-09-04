namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for consumer connector cache.
/// </summary>
public interface IConsumerConnectorCache
{
    /// <summary>
    /// Gets the connector value.
    /// </summary>
    IConsumerConnector Connector { get; }
}
