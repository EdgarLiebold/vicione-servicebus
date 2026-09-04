namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for instance connector cache.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public interface IInstanceConnectorCache<T>
    where T : class
{
    /// <summary>
    /// Gets the connector value.
    /// </summary>
    IInstanceConnector Connector { get; }
}
