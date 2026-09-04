namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for handler connector cache.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public interface IHandlerConnectorCache<T>
    where T : class
{
    /// <summary>
    /// Gets the connector value.
    /// </summary>
    IHandlerConnector<T> Connector { get; }
}
