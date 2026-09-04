namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for saga connector cache.
/// </summary>
public interface ISagaConnectorCache
{
    /// <summary>
    /// Gets the connector value.
    /// </summary>
    ISagaConnector Connector { get; }
}
